namespace rec JetBrains.ReSharper.Plugins.FSharp.Checker

open System
open System.Runtime.InteropServices
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Text
open JetBrains
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.DataFlow
open JetBrains.DocumentModel
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

module FSharpCheckerService =
    let getSourceText (document: IDocument) =
        SourceText.ofString(document.GetText())


[<ShellComponent; AllowNullLiteral>]
type FSharpCheckerService
        (lifetime: Lifetime, logger: ILogger, onSolutionCloseNotifier: OnSolutionCloseNotifier,
         settingsStore: ISettingsStore, settingsSchema: SettingsSchema, reactorMonitor: IFcsReactorMonitor) =

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
                                     keepAllBackgroundSymbolUses = false,
                                     reactorListener = reactorMonitor,
                                     ImplicitlyStartBackgroundWork = enableBgCheck.Value)

            enableBgCheck.Change.Advise_NoAcknowledgement(lifetime, fun (ArgValue enabled) ->
                checker.ImplicitlyStartBackgroundWork <- enabled)

            checker

    do
        onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(lifetime, fun _ ->
            if checker.IsValueCreated then
                checker.Value.InvalidateAll())

    member val FcsProjectProvider = Unchecked.defaultof<IFcsProjectProvider> with get, set

    member x.Checker = checker.Value
    member x.FcsReactorMonitor = reactorMonitor

    member x.ParseFile(path, document, parsingOptions, [<Optional; DefaultParameterValue(false)>] noCache: bool) =
        try
            let source = FSharpCheckerService.getSourceText document
            let fullPath = getFullPath path

            let parseAsync =
                if noCache then
                    x.Checker.ParseFileNoCache(fullPath, source, parsingOptions)
                else
                    x.Checker.ParseFile(fullPath, source, parsingOptions)

            let parseResults = parseAsync.RunAsTask()
            Some parseResults
        with
        | OperationCanceled -> reraise()
        | exn ->
            Util.Logging.Logger.LogException(exn)
            logger.Warn(sprintf "Parse file error, parsing options: %A" parsingOptions)
            None

    member x.ParseFile([<NotNull>] sourceFile: IPsiSourceFile) =
        let parsingOptions = x.FcsProjectProvider.GetParsingOptions(sourceFile)
        x.ParseFile(sourceFile.GetLocation(), sourceFile.Document, parsingOptions)

    member x.ParseAndCheckFile([<NotNull>] file: IPsiSourceFile, opName,
                               [<Optional; DefaultParameterValue(false)>] allowStaleResults) =
        match x.FcsProjectProvider.GetProjectOptions(file) with
        | None -> None
        | Some options ->

        let path = file.GetLocation().FullPath
        let source = FSharpCheckerService.getSourceText file.Document
        logger.Trace("ParseAndCheckFile: start {0}, {1}", path, opName)

        use op = reactorMonitor.MonitorOperation opName

        // todo: don't cancel the computation when file didn't change
        match x.Checker.ParseAndCheckDocument(path, source, options, allowStaleResults, op.OperationName).RunAsTask() with
        | Some (parseResults, checkResults) when parseResults.ParseTree.IsSome ->
            logger.Trace("ParseAndCheckFile: finish {0}, {1}", path, opName)
            Some { ParseResults = parseResults; CheckResults = checkResults }

        | _ ->
            logger.Trace("ParseAndCheckFile: fail {0}, {1}", path, opName)
            None

    member x.TryGetStaleCheckResults([<NotNull>] file: IPsiSourceFile, opName) =
        match x.FcsProjectProvider.GetProjectOptions(file) with
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

    member x.InvalidateFcsProject(fcsProjectOptions: FSharpProjectOptions) =
        if checker.IsValueCreated then
            checker.Value.InvalidateConfiguration(fcsProjectOptions, false)

    /// Use with care: returns wrong symbol inside its non-recursive declaration, see dotnet/fsharp#7694.
    member x.ResolveNameAtLocation(sourceFile: IPsiSourceFile, names, coords, opName) =
        // todo: different type parameters count
        x.ParseAndCheckFile(sourceFile, opName, true)
        |> Option.bind (fun results ->
            let checkResults = results.CheckResults
            let fcsPos = getPosFromCoords coords
            let lineText = sourceFile.Document.GetLineText(coords.Line)

            use op = reactorMonitor.MonitorOperation opName
            checkResults.GetSymbolUseAtLocation(fcsPos.Line, fcsPos.Column, lineText, names, op.OperationName).RunAsTask())

    /// Use with care: returns wrong symbol inside its non-recursive declaration, see dotnet/fsharp#7694.
    member x.ResolveNameAtLocation(sourceFile: IPsiSourceFile, name, coords, opName) =
        x.ResolveNameAtLocation(sourceFile, [name], coords, opName)

    /// Use with care: returns wrong symbol inside its non-recursive declaration, see dotnet/fsharp#7694.
    member x.ResolveNameAtLocation(context: ITreeNode, names, opName) =
        let offset = context.GetNavigationRange().EndOffset - 1
        let coords = offset.ToDocumentCoords()
        x.ResolveNameAtLocation(context.GetSourceFile(), List.ofSeq names, coords, opName)


type FSharpParseAndCheckResults = 
    { ParseResults: FSharpParseFileResults
      CheckResults: FSharpCheckFileResults }


type IFcsProjectProvider =
    abstract GetProjectOptions: IPsiSourceFile -> FSharpProjectOptions option
    abstract GetParsingOptions: IPsiSourceFile -> FSharpParsingOptions
    abstract GetFileIndex: IPsiSourceFile -> int

    // Indicates if implementation file has an associated signature file.
    abstract HasPairFile: IPsiSourceFile -> bool

    /// Returns True when the project has been invalidated.
    abstract InvalidateReferencesToProject: IProject -> bool

    abstract InvalidateDirty: unit -> unit
    abstract ModuleInvalidated: ISignal<IPsiModule>

    /// True when any F# projects are currently known to project options provider after requesting info from FCS.
    abstract HasFcsProjects: bool


type IScriptFcsProjectProvider =
    abstract GetScriptOptions: IPsiSourceFile -> FSharpProjectOptions option
    abstract GetScriptOptions: FileSystemPath * string -> FSharpProjectOptions option
