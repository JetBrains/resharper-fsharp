namespace rec JetBrains.ReSharper.Plugins.FSharp.Checker

open System
open System.Collections.Generic
open System.Runtime.InteropServices
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open JetBrains
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Parts
open JetBrains.Application.Settings
open JetBrains.Application.Threading
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

module FcsCheckerService =
    let getSourceText (document: IDocument) =
        SourceText.ofString(document.GetText())


[<RequireQualifiedAccess>]
type FcsProjectInvalidationType =
    /// Used when invalidation is needed for a project still known to FCS.
    /// Recreates background builder for the project.
    | Invalidate

    /// Used when project options are no longer valid and the corresponding background builder should be removed in FCS.
    | Remove


[<ShellComponent(Instantiation.DemandAnyThreadSafe); AllowNullLiteral>]
type FcsCheckerService(lifetime: Lifetime, logger: ILogger, onSolutionCloseNotifier: OnSolutionCloseNotifier,
        settingsStore: ISettingsStore, locks: IShellLocks) =

    let checker =
        lazy
            Environment.SetEnvironmentVariable("FCS_CheckFileInProjectCacheSize", "20")

            let settingsStoreLive = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide)

            let getSettingProperty name =
                let setting = SettingsUtil.getEntry<FSharpOptions> settingsStore name
                settingsStoreLive.GetValueProperty2(lifetime, setting, null, ApartmentForNotifications.Primary(locks))

            let skipImpl = getSettingProperty "SkipImplementationAnalysis"
            let analyzerProjectReferencesInParallel = getSettingProperty "ParallelProjectReferencesAnalysis"

            let checker =
                FSharpChecker.Create(projectCacheSize = 200,
                                     keepAllBackgroundResolutions = false,
                                     keepAllBackgroundSymbolUses = false,
                                     enablePartialTypeChecking = skipImpl.Value,
                                     parallelReferenceResolution = analyzerProjectReferencesInParallel.Value)

            checker

    do
        onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(lifetime, fun _ ->
            if checker.IsValueCreated then
                checker.Value.InvalidateAll())

    member val FcsProjectProvider = Unchecked.defaultof<IFcsProjectProvider> with get, set

    member x.Checker = checker.Value

    member x.ParseFile(path, document, parsingOptions, [<Optional; DefaultParameterValue(false)>] noCache: bool) =
        try
            locks.AssertReadAccessAllowed()
            let source = FcsCheckerService.getSourceText document
            let fullPath = getFullPath path
            let parseAsync = x.Checker.ParseFile(fullPath, source, parsingOptions, cache = not noCache)
            let parseResults = parseAsync.RunAsTask()
            Some parseResults
        with
        | OperationCanceled -> reraise()
        | exn ->
            Util.Logging.Logger.LogException(exn)
            logger.Warn($"Parse file error, parsing options: %A{parsingOptions}")
            None

    member x.ParseFile([<NotNull>] sourceFile: IPsiSourceFile) =
        let parsingOptions = x.FcsProjectProvider.GetParsingOptions(sourceFile)
        x.ParseFile(sourceFile.GetLocation(), sourceFile.Document, parsingOptions)

    // todo: assert that no modification was done? force pin check results or allow via cookie?
    member x.ParseAndCheckFile([<NotNull>] sourceFile: IPsiSourceFile, opName,
            [<Optional; DefaultParameterValue(false)>] allowStaleResults) =
        match PinTypeCheckResultsCookie.PinnedResults with
        | Some(parseResults, checkResults) -> Some({ ParseResults = parseResults; CheckResults = checkResults })
        | _ ->

        ProhibitTypeCheckCookie.AssertTypeCheckIsAllowed()
        locks.AssertReadAccessAllowed()

        let psiModule = sourceFile.PsiModule
        match x.FcsProjectProvider.GetFcsProject(psiModule) with
        | None -> None
        | Some fcsProject ->

        let options = fcsProject.ProjectOptions
        if not (fcsProject.IsKnownFile(sourceFile)) && not options.UseScriptResolutionRules then None else

        x.FcsProjectProvider.PrepareAssemblyShim(psiModule)

        let path = sourceFile.GetLocation().FullPath
        let source = FcsCheckerService.getSourceText sourceFile.Document
        logger.Trace("ParseAndCheckFile: start {0}, {1}", path, opName)

        // todo: don't cancel the computation when file didn't change
        match x.Checker.ParseAndCheckDocument(path, source, options, allowStaleResults, opName).RunAsTask() with
        | Some (parseResults, checkResults) ->
            logger.Trace("ParseAndCheckFile: finish {0}, {1}", path, opName)
            Some { ParseResults = parseResults; CheckResults = checkResults }

        | _ ->
            logger.Trace("ParseAndCheckFile: fail {0}, {1}", path, opName)
            None

    member x.PinCheckResults(sourceFile, prohibitTypeCheck, opName) =
        match x.ParseAndCheckFile(sourceFile, opName) with
        | Some(parseAndCheckResults) ->
            new PinTypeCheckResultsCookie(sourceFile, parseAndCheckResults.ParseResults, parseAndCheckResults.CheckResults, prohibitTypeCheck) :> IDisposable
        | _ -> { new IDisposable with member this.Dispose() = () }

    member x.PinCheckResults(results, sourceFile, prohibitTypeCheck, opName) =
        match results with
        | Some results ->
            new PinTypeCheckResultsCookie(sourceFile, results.ParseResults, results.CheckResults, prohibitTypeCheck) :> IDisposable
        | _ -> { new IDisposable with member this.Dispose() = () }

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

    member x.GetCachedScriptOptions(path) =
        if checker.IsValueCreated then
            checker.Value.GetCachedScriptOptions(path)
        else None
    
    member x.InvalidateFcsProject(projectOptions: FSharpProjectOptions, invalidationType: FcsProjectInvalidationType) =
        if checker.IsValueCreated then
            match invalidationType with
            | FcsProjectInvalidationType.Invalidate ->
                logger.Trace("Remove FcsProject in FCS: {0}", projectOptions.ProjectFileName)
                checker.Value.ClearCache(Seq.singleton projectOptions)
            | FcsProjectInvalidationType.Remove ->
                logger.Trace("Invalidate FcsProject in FCS: {0}", projectOptions.ProjectFileName)
                checker.Value.InvalidateConfiguration(projectOptions)

    /// Use with care: returns wrong symbol inside its non-recursive declaration, see dotnet/fsharp#7694.
    member x.ResolveNameAtLocation(sourceFile: IPsiSourceFile, names, coords, resolveExpr: bool, opName) =
        // todo: different type parameters count
        x.ParseAndCheckFile(sourceFile, opName, true)
        |> Option.bind (fun results ->
            let checkResults = results.CheckResults
            let fcsPos = getPosFromCoords coords
            if resolveExpr then
                checkResults.ResolveNamesAtLocation(fcsPos, names)
            else
                let lineText = sourceFile.Document.GetLineText(coords.Line)
                checkResults.GetSymbolUseAtLocation(fcsPos.Line, fcsPos.Column, lineText, names))

    /// Use with care: returns wrong symbol inside its non-recursive declaration, see dotnet/fsharp#7694.
    member x.ResolveNameAtLocation(sourceFile: IPsiSourceFile, name: string, coords, resolveExpr, opName) =
        x.ResolveNameAtLocation(sourceFile, [name], coords, resolveExpr, opName)

    /// Use with care: returns wrong symbol inside its non-recursive declaration, see dotnet/fsharp#7694.
    member x.ResolveNameAtLocation(context: ITreeNode, names, resolveExpr, opName) =
        let offset = context.GetNavigationRange().EndOffset - 1
        let coords = offset.ToDocumentCoords()
        x.ResolveNameAtLocation(context.GetSourceFile(), List.ofSeq names, coords, resolveExpr, opName)


type FSharpParseAndCheckResults =
    { ParseResults: FSharpParseFileResults
      CheckResults: FSharpCheckFileResults }


type ReferencedModule =
    { ReferencingProjects: HashSet<FcsProjectKey> }

module ReferencedModule =
    let create () =
        { ReferencingProjects = HashSet() }


type IFcsProjectProvider =
    abstract GetFcsProject: psiModule: IPsiModule -> FcsProject option
    abstract GetPsiModule: outputPath: VirtualFileSystemPath -> IPsiModule option

    abstract GetProjectOptions: sourceFile: IPsiSourceFile -> FSharpProjectOptions option
    abstract GetProjectOptions: psiModule: IPsiModule -> FSharpProjectOptions option

    abstract GetFileIndex: IPsiSourceFile -> int
    abstract GetParsingOptions: sourceFile: IPsiSourceFile -> FSharpParsingOptions

    // Indicates if implementation file has an associated signature file.
    abstract HasPairFile: IPsiSourceFile -> bool

    /// Returns True when the project has been invalidated.
    abstract InvalidateReferencesToProject: IProject -> bool

    abstract ProjectRemoved: ISignal<FcsProjectKey * FcsProject>

    abstract PrepareAssemblyShim: psiModule: IPsiModule -> unit 

    abstract GetReferencedModule: projectKey: FcsProjectKey -> ReferencedModule option 
    abstract GetAllReferencedModules: unit -> KeyValuePair<FcsProjectKey, ReferencedModule> seq

    /// True when any F# projects are currently known to project options provider after requesting info from FCS.
    abstract HasFcsProjects: bool
    abstract GetAllFcsProjects: unit -> FcsProject seq


type IScriptFcsProjectProvider =
    abstract GetFcsProject: IPsiSourceFile -> FcsProject option
    abstract GetScriptOptions: IPsiSourceFile -> FSharpProjectOptions option
    abstract GetScriptOptions: VirtualFileSystemPath * string -> FSharpProjectOptions option
    abstract OptionsUpdated: Signal<VirtualFileSystemPath * FSharpProjectOptions>
    abstract SyncUpdate: bool


[<AutoOpen>]
module ProjectOptions =
    [<RequireQualifiedAccess>]
    module ImplicitDefines =
        // todo: don't pass to FCS, only use in internal lexing; these defines added by FCS too
        let sourceDefines = [ "EDITING"; "COMPILED" ]
        let scriptDefines = [ "EDITING"; "INTERACTIVE" ]

        let getImplicitDefines isScript =
            if isScript then scriptDefines else sourceDefines

    let sandboxParsingOptions =
        // todo: use script defines in interactive?
        { FSharpParsingOptions.Default with
            ConditionalDefines = ImplicitDefines.sourceDefines
            SourceFiles = [| "Sandbox.fs" |] }
