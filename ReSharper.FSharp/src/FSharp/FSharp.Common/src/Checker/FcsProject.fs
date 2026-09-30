#nowarn FS0057

namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System
open System.Collections.Generic
open System.IO
open FSharp.Compiler.AbstractIL.ILBinaryReader
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.CodeAnalysis.ProjectSnapshot
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds

type FcsProjectKey =
    { Project: IProject
      TargetFrameworkId: TargetFrameworkId }

    static member Create(psiModule: IPsiModule) =
        { Project = psiModule.ContainingProjectModule :?> _
          TargetFrameworkId = psiModule.TargetFrameworkId }

    static member Create(project, targetFrameworkId) =
        { Project = project
          TargetFrameworkId = targetFrameworkId }

type FcsProjectOptions =
    | FcsProjectOptions of FSharpProjectOptions * FSharpParsingOptions
    | FcsProjectSnapshot of FSharpProjectSnapshot

    member x.Stamp =
        match x with
        | FcsProjectOptions(options, _) -> options.Stamp
        | FcsProjectSnapshot(snapshot) -> snapshot.Stamp

    member x.LoadTime =
        match x with
        | FcsProjectOptions(options, _) -> options.LoadTime
        | FcsProjectSnapshot(snapshot) -> snapshot.LoadTime

    member x.ProjectFileName =
        match x with
        | FcsProjectOptions(options, _) -> options.ProjectFileName
        | FcsProjectSnapshot(snapshot) -> snapshot.ProjectFileName

    member x.UseScriptResolutionRules =
        match x with
        | FcsProjectOptions(options, _) -> options.UseScriptResolutionRules
        | FcsProjectSnapshot(snapshot) -> snapshot.UseScriptResolutionRules

    member x.OriginalLoadReferences =
        match x with
        | FcsProjectOptions(options, _) -> options.OriginalLoadReferences
        | FcsProjectSnapshot(snapshot) -> snapshot.OriginalLoadReferences

    member x.SourceFiles: string array =
        match x with
        | FcsProjectOptions(options, _) -> options.SourceFiles
        | FcsProjectSnapshot(snapshot) -> snapshot.SourceFiles |> Seq.map _.FileName |> Seq.toArray

    member x.OtherOptions: string seq =
        match x with
        | FcsProjectOptions(options, _) -> options.OtherOptions
        //TODO: change type to array in FCS 
        | FcsProjectSnapshot(snapshot) -> snapshot.OtherOptions

    member x.ParsingOptions: FSharpParsingOptions =
        match x with
        | FcsProjectOptions(_, parsingOptions) -> parsingOptions
        | FcsProjectSnapshot snapshot ->
        { FSharpParsingOptions.Default with
            SourceFiles = x.SourceFiles
            ConditionalDefines = x.ConditionalDefines
            IsInteractive = snapshot.UseScriptResolutionRules
            //LangVersionText = TODO
            IsExe = false } //TODO: is exe

    member x.ConditionalDefines: string list =
        match x with
        | FcsProjectOptions(_, options) -> options.ConditionalDefines
        //TODO: FCS doesn't provide this info
        | FcsProjectSnapshot(snapshot) -> [
            if snapshot.UseScriptResolutionRules then
                "INTERACTIVE"
            else
                "COMPILED"
        ]

    member x.AreSameForChecking(y: FcsProjectOptions) =
        let inline arrayEq a1 a2 = Array.length a1 = Array.length a2 && Array.forall2 (=) a1 a2

        if x.ProjectFileName <> y.ProjectFileName ||
           not (arrayEq x.SourceFiles y.SourceFiles) ||
           x.OtherOptions <> y.OtherOptions
           then false else

        if x.UseScriptResolutionRules then x.OriginalLoadReferences = y.OriginalLoadReferences else

        match x, y with
        | FcsProjectOptions(x, _), FcsProjectOptions(y, _) ->
            x.ReferencedProjects.Length = x.ReferencedProjects.Length &&
            (y.ReferencedProjects, y.ReferencedProjects)
            ||> Array.forall2 (fun r1 r2 ->
                match r1, r2 with
                | FSharpReferencedProject.FSharpReference (_, r1),
                  FSharpReferencedProject.FSharpReference (_, r2) ->
                    r1.Stamp = r2.Stamp

                | FSharpReferencedProject.ILModuleReference(_, _, getReader1),
                  FSharpReferencedProject.ILModuleReference(_, _, getReader2) ->
                    getReader1 () = getReader2 ()

                | _ -> false
            )

        | FcsProjectSnapshot(x), FcsProjectSnapshot(y) ->
            x.ReferencesOnDisk = y.ReferencesOnDisk && //TODO: array
            x.ReferencedProjects.Length = x.ReferencedProjects.Length &&
            (y.ReferencedProjects, y.ReferencedProjects)
            ||> List.forall2 (fun r1 r2 -> arrayEq r1.Version r2.Version)

        | _ -> false

type FcsReferencedProject =
    | FcsReference of FcsProject
    | FcsILModuleReference of x: (string * (unit -> DateTime) * (unit -> ILModuleReader))

and FcsProject =
    { OutputPath: VirtualFileSystemPath
      Options: FcsProjectOptions
      FileIndices: IDictionary<VirtualFileSystemPath, int>
      ImplementationFilesWithSignatures: ISet<VirtualFileSystemPath>
      ReferencedModules: ISet<FcsProjectKey> }

    static member CreateStubFrom(options: FSharpProjectOptions) = {
        OutputPath = VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.Local)
        Options = FcsProjectOptions.FcsProjectOptions(options, FSharpParsingOptions.Default)
        FileIndices = Dictionary()
        ImplementationFilesWithSignatures = HashSet()
        ReferencedModules = HashSet()
    }

    member x.IsKnownFile(sourceFile: IPsiSourceFile) =
        let path = sourceFile.GetLocation()
        x.FileIndices.ContainsKey(path)

    member x.GetIndex(sourceFile: IPsiSourceFile) =
        let path = sourceFile.GetLocation()
        tryGetValue path x.FileIndices |> Option.defaultValue -1

    member x.AreSameForChecking(y: FcsProject) =
        x.Options.AreSameForChecking(y.Options)

    member x.WithReferences(moduleReferences: FcsProjectKey array, mapper: FcsProjectKey -> FcsReferencedProject option) =
        let applicableReferences = moduleReferences |> Seq.choose mapper

        let options =
            match x.Options with
            | FcsProjectOptions(projectOptions, parsingOptions) ->
                let references =
                    applicableReferences
                    |> Seq.map (function
                        | FcsILModuleReference foo -> FSharpReferencedProject.ILModuleReference foo
                        | FcsReference { Options = FcsProjectOptions(options, _); OutputPath = outputPath } ->
                            FSharpReferencedProject.FSharpReference(outputPath.FullPath, options)
                        | _ -> failwith "Expecting FcsReference with FcsProjectOptions"
                    )
                    |> Seq.toArray

                FcsProjectOptions({ projectOptions with ReferencedProjects = references }, parsingOptions)

            | FcsProjectSnapshot projectSnapshot ->
                let references =
                    applicableReferences
                    |> Seq.map (function
                        | FcsILModuleReference foo -> FSharpReferencedProjectSnapshot.ILModuleReference foo
                        | FcsReference { Options = FcsProjectSnapshot(snapshot); OutputPath = outputPath } ->
                            FSharpReferencedProjectSnapshot.FSharpReference(outputPath.FullPath, snapshot)
                        | _ -> failwith "Expecting FcsReference with FcsProjectSnapshot"
                    )
                    |> Seq.toList

                //TODO: add a simpler ctor in FCS 
                FSharpProjectSnapshot.Create(
                    projectSnapshot.ProjectFileName,
                    projectSnapshot.OutputFileName,
                    projectSnapshot.ProjectId,
                    projectSnapshot.SourceFiles,
                    projectSnapshot.ReferencesOnDisk,
                    projectSnapshot.OtherOptions,
                    references,
                    projectSnapshot.IsIncompleteTypeCheckEnvironment,
                    projectSnapshot.UseScriptResolutionRules,
                    projectSnapshot.LoadTime,
                    projectSnapshot.UnresolvedReferences,
                    projectSnapshot.OriginalLoadReferences,
                    projectSnapshot.Stamp)
                |> FcsProjectSnapshot

        { x with ReferencedModules = HashSet(moduleReferences); Options = options }

    member x.TestDump(writer: TextWriter) =
        let options = x.Options

        writer.WriteLine($"Project file: {options.ProjectFileName}")
        writer.WriteLine($"Stamp: {options.Stamp}")
        writer.WriteLine($"Load time: {options.LoadTime}")

        writer.WriteLine("Source files:")
        for sourceFile in options.SourceFiles do
            writer.WriteLine($"  {sourceFile}")

        writer.WriteLine("Other options:")
        for option in options.OtherOptions do
            writer.WriteLine($"  {option}")

        writer.WriteLine("Referenced projects:")
        
        match options with
        | FcsProjectOptions(options, _) ->
            for referencedProject in options.ReferencedProjects do
                let stamp =
                    match referencedProject with
                    | FSharpReferencedProject.FSharpReference(_, options) -> $"{options.Stamp}: "
                    | _ -> ""
                writer.WriteLine($"  {stamp}{referencedProject.OutputFile}")

        | FcsProjectSnapshot projectSnapshot ->
            for referencedProject in projectSnapshot.ReferencedProjects do
                let stamp =
                    match referencedProject with
                    | FSharpReferencedProjectSnapshot.FSharpReference(_, options) -> $"{options.Stamp}: "
                    | _ -> ""
                writer.WriteLine($"  {stamp}{referencedProject.OutputFile}")

        writer.WriteLine()
