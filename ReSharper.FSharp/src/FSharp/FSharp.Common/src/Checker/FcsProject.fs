#nowarn FS0057

namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System.Collections.Generic
open System.IO
open System.Linq
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
        | FcsProjectSnapshot(snapshot) -> snapshot.OtherOptions

    member x.ParsingOptions: FSharpParsingOptions =
        match x with
        | FcsProjectOptions(_, parsingOptions) -> parsingOptions
        | FcsProjectSnapshot snapshot ->
        { FSharpParsingOptions.Default with
            SourceFiles = x.SourceFiles
            ConditionalDefines = x.ConditionalDefines
            IsInteractive = false
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
        // let arrayEq a1 a2 =
        //     Array.length a1 = Array.length a2 && Array.forall2 (=) a1 a2

        x.ProjectFileName = y.ProjectFileName &&
        x.SourceFiles = y.SourceFiles &&
        x.OtherOptions = y.OtherOptions &&
        
        (x.UseScriptResolutionRules && x.OriginalLoadReferences = y.OriginalLoadReferences ||

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
            x.ReferencedProjects.Length = x.ReferencedProjects.Length &&
            (y.ReferencedProjects, y.ReferencedProjects)
            ||> List.forall2 (fun r1 r2 ->
                match r1, r2 with
                | FSharpReferencedProjectSnapshot.FSharpReference (_, r1),
                  FSharpReferencedProjectSnapshot.FSharpReference (_, r2) ->
                    r1.Stamp = r2.Stamp

                | FSharpReferencedProjectSnapshot.ILModuleReference(_, _, getReader1),
                  FSharpReferencedProjectSnapshot.ILModuleReference(_, _, getReader2) ->
                    getReader1 () = getReader2 ()

                | _ -> false
            )

        | _ -> false)

type FcsProject =
    { OutputPath: VirtualFileSystemPath
      Options: FcsProjectOptions
      FileIndices: IDictionary<VirtualFileSystemPath, int>
      ImplementationFilesWithSignatures: ISet<VirtualFileSystemPath>
      ReferencedModules: ISet<FcsProjectKey> }

    static member Create(options) = {
        OutputPath = VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.Local) //TODO: remove as a redundant
        Options = options
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

    member x.WithReferences(moduleReferences: FcsProjectKey seq, mapper) =
        let moduleReferences = moduleReferences.ToArray()

        let options =
            match x.Options with
            | FcsProjectOptions(projectOptions, parsingOptions) ->
                let references =
                    moduleReferences
                    |> Seq.choose (fun x ->
                        match mapper x with
                        | Choice1Of3 fcsProject ->
                            match fcsProject.Options with
                            | FcsProjectOptions(projectOptions, _) ->
                                FSharpReferencedProject.FSharpReference(fcsProject.OutputPath.FullPath, projectOptions) |> Some
                            | _ -> None
                        | Choice2Of3 foo -> FSharpReferencedProject.ILModuleReference foo |> Some
                        | _ -> None
                    )
                    |> Seq.toArray

                FcsProjectOptions({ projectOptions with ReferencedProjects = references }, parsingOptions)

            | FcsProjectSnapshot projectSnapshot ->
                let references =
                    moduleReferences
                    |> Seq.choose (fun x ->
                        match mapper x with
                        | Choice1Of3 fcsProject ->
                            match fcsProject.Options with
                            | FcsProjectSnapshot(snapshot) ->
                                FSharpReferencedProjectSnapshot.FSharpReference(fcsProject.OutputPath.FullPath, snapshot) |> Some
                            | _ -> None
                        | Choice2Of3 foo -> FSharpReferencedProjectSnapshot.ILModuleReference foo |> Some
                        | _ -> None
                    )
                    |> Seq.toList

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
