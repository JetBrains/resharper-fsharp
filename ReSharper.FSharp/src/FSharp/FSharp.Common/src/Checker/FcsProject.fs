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

[<AutoOpen>]
module FSharpProjectSnapshotExtensions =
    type FSharpProjectSnapshot with
        //TODO: add a simpler ctor in FCS
        member x.With(?sourceFiles: FSharpFileSnapshot list,
                      ?referencedProjects: FSharpReferencedProjectSnapshot list,
                      ?stamp: int64 option) =
            FSharpProjectSnapshot.Create(
                x.ProjectFileName,
                x.OutputFileName,
                x.ProjectId,
                defaultArg sourceFiles x.SourceFiles,
                x.ReferencesOnDisk,
                x.OtherOptions,
                defaultArg referencedProjects x.ReferencedProjects,
                x.IsIncompleteTypeCheckEnvironment,
                x.UseScriptResolutionRules,
                x.LoadTime,
                x.UnresolvedReferences,
                x.OriginalLoadReferences,
                defaultArg stamp x.Stamp)

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

    member x.ReferencePaths: string seq =
        match x with
        | FcsProjectOptions(options, _) ->
            options.OtherOptions
            |> Seq.choose (fun o -> if o.StartsWith("-r:", StringComparison.Ordinal) then Some(o.Substring(3)) else None)

        | FcsProjectSnapshot(snapshot) -> snapshot.ReferencesOnDisk |> Seq.map _.Path

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

        let referencesOnDisk (options: FcsProjectOptions) =
            match options with
            | FcsProjectSnapshot(snapshot) -> snapshot.ReferencesOnDisk
            | _ -> []

        if x.ProjectFileName <> y.ProjectFileName ||
           not (arrayEq x.SourceFiles y.SourceFiles) ||
           x.OtherOptions <> y.OtherOptions
           then false else

        if x.UseScriptResolutionRules then
            x.OriginalLoadReferences = y.OriginalLoadReferences &&
            referencesOnDisk x = referencesOnDisk y else

        match x, y with
        | FcsProjectOptions(x, _), FcsProjectOptions(y, _) ->
            x.ReferencedProjects.Length = y.ReferencedProjects.Length &&
            (x.ReferencedProjects, y.ReferencedProjects)
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
            x.ReferencesOnDisk = y.ReferencesOnDisk &&
            x.ReferencedProjects.Length = y.ReferencedProjects.Length &&
            (x.ReferencedProjects, y.ReferencedProjects)
            ||> List.forall2 (fun r1 r2 ->
                match r1, r2 with
                | FSharpReferencedProjectSnapshot.FSharpReference(_, r1),
                  FSharpReferencedProjectSnapshot.FSharpReference(_, r2) ->
                    r1.Stamp = r2.Stamp

                | _ -> r1 = r2
            )

        | _ -> false

[<RequireQualifiedAccess>]
type FcsReferencedModule =
    | Project of FcsProject
    | OutputAssembly of path: string * getTimestamp: (unit -> DateTime) * getReader: (unit -> ILModuleReader)

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

    member x.WithUpdatedFile(path: VirtualFileSystemPath) =
        match x.Options with
        | FcsProjectSnapshot(snapshot) when x.FileIndices.ContainsKey(path) ->
            let snapshot = snapshot.Replace([FSharpFileSnapshot.CreateFromFileSystem(path.FullPath)])
            { x with Options = FcsProjectSnapshot(snapshot) }

        | _ -> x

    member x.WithUpdateReferencedProject(outputPath: string, referencedSnapshot: FSharpProjectSnapshot) =
        match x.Options with
        | FcsProjectSnapshot(snapshot) ->
            let referencedProjects =
                snapshot.ReferencedProjects
                |> List.map (function
                    | FSharpReferencedProjectSnapshot.FSharpReference(path, _) when path = outputPath ->
                        FSharpReferencedProjectSnapshot.FSharpReference(path, referencedSnapshot)
                    | reference -> reference)

            { x with Options = FcsProjectSnapshot(snapshot.With(referencedProjects = referencedProjects)) }

        | _ -> x

    member x.WithReferences(moduleReferences: FcsProjectKey array, mapper: FcsProjectKey -> FcsReferencedModule option) =
        let applicableReferences = moduleReferences |> Seq.choose mapper

        let options =
            match x.Options with
            | FcsProjectOptions(projectOptions, parsingOptions) ->
                let references =
                    applicableReferences
                    |> Seq.map (function
                        | FcsReferencedModule.OutputAssembly(path, getTimestamp, getReader) ->
                            FSharpReferencedProject.ILModuleReference(path, getTimestamp, getReader)
                        | FcsReferencedModule.Project { Options = FcsProjectOptions(options, _); OutputPath = outputPath } ->
                            FSharpReferencedProject.FSharpReference(outputPath.FullPath, options)
                        | _ -> failwith "Expecting referenced project with FcsProjectOptions"
                    )
                    |> Seq.toArray

                FcsProjectOptions({ projectOptions with ReferencedProjects = references }, parsingOptions)

            | FcsProjectSnapshot projectSnapshot ->
                let references =
                    applicableReferences
                    |> Seq.map (function
                        | FcsReferencedModule.OutputAssembly(path, getTimestamp, getReader) ->
                            FSharpReferencedProjectSnapshot.ILModuleReference(path, getTimestamp, getReader)
                        | FcsReferencedModule.Project { Options = FcsProjectSnapshot(snapshot); OutputPath = outputPath } ->
                            FSharpReferencedProjectSnapshot.FSharpReference(outputPath.FullPath, snapshot)
                        | _ -> failwith "Expecting referenced project with FcsProjectSnapshot"
                    )
                    |> Seq.toList

                FcsProjectSnapshot(projectSnapshot.With(referencedProjects = references))

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
                    | FSharpReferencedProjectSnapshot.FSharpReference(_, snapshot) -> $"{snapshot.Stamp}: "
                    | _ -> ""
                writer.WriteLine($"  {stamp}{referencedProject.OutputFile}")

        writer.WriteLine()
