namespace rec JetBrains.ReSharper.Plugins.FSharp.Checker

open System
open System.Collections.Generic
open System.Runtime.InteropServices
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Text
open JetBrains
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.DataFlow
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<ShellComponent; AllowNullLiteral>]
type FSharpCheckerService
        (lifetime, logger: ILogger, onSolutionCloseNotifier: OnSolutionCloseNotifier, settingsStore: ISettingsStore,
         settingsSchema: SettingsSchema) =

    let checker =
        Environment.SetEnvironmentVariable("FCS_CheckFileInProjectCacheSize", "20")

        let settingsEntry =
            let settingsKey = settingsSchema.GetKey<FSharpOptions>()
            settingsKey.TryFindEntryByMemberName("BackgroundTypeCheck") :?> SettingsScalarEntry

        let enableBgCheck =
            settingsStore
                .BindToContextLive(lifetime, ContextRange.ApplicationWide)
                .GetValueProperty(lifetime, settingsEntry, null)

        lazy
            let checker =
                FSharpChecker.Create(projectCacheSize = 200,
                                     keepAllBackgroundResolutions = false,
                                     ImplicitlyStartBackgroundWork = enableBgCheck.Value)

            enableBgCheck.Change.Advise_NoAcknowledgement(lifetime, fun (ArgValue enabled) ->
                checker.ImplicitlyStartBackgroundWork <- enabled)

            checker

    do
        onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(lifetime, fun _ ->
            if checker.IsValueCreated then
                checker.Value.InvalidateAll())

    member val OptionsProvider = Unchecked.defaultof<IFSharpProjectOptionsProvider> with get, set
    member x.Checker = checker.Value

    member x.ParseFile(path: FileSystemPath, document: IDocument, parsingOptions: FSharpParsingOptions) =
        let source = SourceText.ofString (document.GetText())
        try
            let parseResults = x.Checker.ParseFile(path.FullPath, source, parsingOptions).RunAsTask() 
            Some parseResults
        with
        | OperationCanceled -> reraise()
        | exn ->
            Util.Logging.Logger.LogException(exn)
            logger.Warn(sprintf "Parse file error, parsing options: %A" parsingOptions)
            None

    member x.ParseFile([<NotNull>] sourceFile: IPsiSourceFile) =
        let parsingOptions = x.OptionsProvider.GetParsingOptions(sourceFile)
        x.ParseFile(sourceFile.GetLocation(), sourceFile.Document, parsingOptions)

    member x.GetParsingOptions([<NotNull>] sourceFile: IPsiSourceFile) =
        if isNull sourceFile then { FSharpParsingOptions.Default with SourceFiles = [| "Sandbox.fs" |] } else 
        x.OptionsProvider.GetParsingOptions(sourceFile)

    member x.HasPairFile([<NotNull>] file: IPsiSourceFile) =
        x.OptionsProvider.HasPairFile(file)

    member x.GetDefines([<CanBeNull>] sourceFile: IPsiSourceFile) =
        if isNull sourceFile then [] else

        let isScript = sourceFile.LanguageType.Is<FSharpScriptProjectFileType>()
        let implicitDefines = getImplicitDefines isScript
        let projectDefines = x.OptionsProvider.GetParsingOptions(sourceFile).ConditionalCompilationDefines
        implicitDefines @ projectDefines

    member x.ParseAndCheckFile([<NotNull>] file: IPsiSourceFile, opName,
                               [<Optional; DefaultParameterValue(false)>] allowStaleResults) =
        match x.OptionsProvider.GetProjectOptions(file) with
        | None -> None
        | Some options ->

        let path = file.GetLocation().FullPath
        let source = SourceText.ofString (file.Document.GetText())
        logger.Trace("ParseAndCheckFile: start {0}, {1}", path, opName)

        // todo: don't cancel the computation when file didn't change
        match x.Checker.ParseAndCheckDocument(path, source, options, allowStaleResults, opName).RunAsTask() with
        | Some (parseResults, checkResults) when parseResults.ParseTree.IsSome ->
            logger.Trace("ParseAndCheckFile: finish {0}, {1}", path, opName)
            Some { ParseResults = parseResults; CheckResults = checkResults }

        | _ ->
            logger.Trace("ParseAndCheckFile: fail {0}, {1}", path, opName)
            None

    member x.TryGetStaleCheckResults([<NotNull>] file: IPsiSourceFile, opName) =
        match x.OptionsProvider.GetProjectOptions(file) with
        | None -> None
        | Some options ->

        let path = file.GetLocation().FullPath
        logger.Trace("TryGetStaleCheckResults: start {0}, {1}", path, opName)

        match x.Checker.TryGetRecentCheckResultsForFile(path, options) with
        | Some (_, checkResults, _) ->
            logger.Trace("TryGetStaleCheckResults: finish {0}, {1}", path, opName)
            Some checkResults

        | _ ->
            logger.Trace("TryGetStaleCheckResults: fail {0}, {1}", path, opName)
            None

    member x.InvalidateFSharpProject(fsProject: FSharpProject) =
        if checker.IsValueCreated then
            checker.Value.InvalidateConfiguration(fsProject.ProjectOptions, false)

    /// Use with care: returns wrong symbol inside its non-recursive declaration, see dotnet/fsharp#7694.
    member x.ResolveNameAtLocation(sourceFile: IPsiSourceFile, names, coords, opName) =
        // todo: different type parameters count
        x.ParseAndCheckFile(sourceFile, opName, true)
        |> Option.bind (fun results ->
            let checkResults = results.CheckResults
            let fcsPos = getPosFromCoords coords
            let lineText = sourceFile.Document.GetLineText(coords.Line)
            checkResults.GetSymbolUseAtLocation(fcsPos.Line, fcsPos.Column, lineText, names, opName).RunAsTask())

    /// Use with care: returns wrong symbol inside its non-recursive declaration, see dotnet/fsharp#7694.
    member x.ResolveNameAtLocation(sourceFile: IPsiSourceFile, name, coords, opName) =
        x.ResolveNameAtLocation(sourceFile, [name], coords, opName)

    /// Use with care: returns wrong symbol inside its non-recursive declaration, see dotnet/fsharp#7694.
    member x.ResolveNameAtLocation(context: ITreeNode, names, opName) =
        let sourceFile = context.GetSourceFile()
        let names = List.ofSeq names
        let coords = context.GetNavigationRange().StartOffset.ToDocumentCoords()
        x.ResolveNameAtLocation(sourceFile, names, coords, opName)

    /// Use with care: returns wrong symbol inside its non-recursive declaration, see dotnet/fsharp#7694.
    member x.ResolveNameAtLocation(reference: TreeReferenceBase<_>, opName) =
        let context = reference.GetElement()
        let names = reference.GetAllNames().ResultingList()
        x.ResolveNameAtLocation(context, names, opName)


[<AutoOpen>]
module ImplicitDefines =
    let sourceDefines = [ "EDITING"; "COMPILED" ]
    let scriptDefines = [ "EDITING"; "INTERACTIVE" ]

    let getImplicitDefines isScript =
        if isScript then scriptDefines else sourceDefines


type FSharpProject =
    { ProjectOptions: FSharpProjectOptions
      ParsingOptions: FSharpParsingOptions
      FileIndices: IDictionary<FileSystemPath, int>
      ImplFilesWithSigs: ISet<FileSystemPath> }

    member x.ContainsFile(file: IPsiSourceFile) =
        x.FileIndices.ContainsKey(file.GetLocation())


type FSharpParseAndCheckResults = 
    { ParseResults: FSharpParseFileResults
      CheckResults: FSharpCheckFileResults }


type IFSharpProjectOptionsProvider =
    abstract GetProjectOptions: IPsiSourceFile -> FSharpProjectOptions option
    abstract GetParsingOptions: IPsiSourceFile -> FSharpParsingOptions
    abstract GetFileIndex: IPsiSourceFile -> int
    abstract HasPairFile: IPsiSourceFile -> bool
    abstract ModuleInvalidated: ISignal<IPsiModule>


type IFSharpScriptProjectOptionsProvider =
    abstract GetScriptOptions: IPsiSourceFile -> FSharpProjectOptions option
    abstract GetScriptOptions: FileSystemPath * string -> FSharpProjectOptions option
