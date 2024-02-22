namespace rec JetBrains.ReSharper.Plugins.FSharp.Checker

#nowarn "57"

open System
open System.Collections.Generic
open System.IO
open System.Runtime.InteropServices
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open JetBrains
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
open JetBrains.Application.Settings
open JetBrains.Application.Threading
open JetBrains.DataFlow
open JetBrains.DocumentModel
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CSharp
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.VB
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

module FcsCheckerService =
    let getSourceText (document: IDocument) =
        SourceText.ofString(document.GetText())


type FcsProject =
    { OutputPath: VirtualFileSystemPath
      ProjectSnapshot: FSharpProjectSnapshot
      ParsingOptions: FSharpParsingOptions
      FileIndices: IDictionary<VirtualFileSystemPath, int>
      ImplementationFilesWithSignatures: ISet<VirtualFileSystemPath>
      ReferencedModules: ISet<FcsProjectKey> }

    member x.IsKnownFile(sourceFile: IPsiSourceFile) =
        let path = sourceFile.GetLocation()
        x.FileIndices.ContainsKey(path)

    member x.GetIndex(sourceFile: IPsiSourceFile) =
        let path = sourceFile.GetLocation()
        tryGetValue path x.FileIndices |> Option.defaultValue -1

    member x.TestDump(writer: TextWriter) =
        let projectSnapshot = x.ProjectSnapshot
        let (ProjectSnapshot.FSharpProjectIdentifier(projectFileName, _)) = projectSnapshot.Identifier

        writer.WriteLine($"Project file: {projectFileName}")
        // TODO: the ProjectSnapshot exposes less information than the ProjectOptions
        
        // writer.WriteLine($"Stamp: {projectSnapshot.}")
        // writer.WriteLine($"Load time: {projectSnapshot.LoadTime}")
        //
        // writer.WriteLine("Source files:")
        // for sourceFile in projectSnapshot.SourceFiles do
        //     writer.WriteLine($"  {sourceFile}")
        //
        // writer.WriteLine("Other options:")
        // for option in projectSnapshot.OtherOptions do
        //     writer.WriteLine($"  {option}")
        //
        // writer.WriteLine("Referenced projects:")
        // for referencedProject in projectSnapshot.ReferencedProjects do
        //     writer.WriteLine($"  {referencedProject.OutputFile}")

        writer.WriteLine()


[<RequireQualifiedAccess>]
type FcsProjectInvalidationType =
    /// Used when invalidation is needed for a project still known to FCS.
    /// Recreates background builder for the project.
    | Invalidate

    /// Used when project options are no longer valid and the corresponding background builder should be removed in FCS.
    | Remove


[<ShellComponent; AllowNullLiteral>]
type FcsCheckerService(lifetime: Lifetime, logger: ILogger, onSolutionCloseNotifier: OnSolutionCloseNotifier,
        settingsStore: ISettingsStore, locks: IShellLocks, configurations: RunsProducts.ProductConfigurations) =

    let settingsStoreLive = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide)

    let getSettingProperty name =
        let setting = SettingsUtil.getEntry<FSharpOptions> settingsStore name
        settingsStoreLive.GetValueProperty(lifetime, setting, null)

    let useTransparentCompiler = (getSettingProperty "UseTransparentCompiler").Value
    
    let checker =
        Environment.SetEnvironmentVariable("FCS_CheckFileInProjectCacheSize", "20")
        let skipImpl = getSettingProperty "SkipImplementationAnalysis"
        let analyzerProjectReferencesInParallel = getSettingProperty "ParallelProjectReferencesAnalysis"
    
        lazy
            let checker =
                FSharpChecker.Create(projectCacheSize = 200,
                                     keepAllBackgroundResolutions = false,
                                     keepAllBackgroundSymbolUses = false,
                                     enablePartialTypeChecking = skipImpl.Value,
                                     parallelReferenceResolution = analyzerProjectReferencesInParallel.Value,
                                     useTransparentCompiler = useTransparentCompiler)

            checker

    do
        onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(lifetime, fun _ ->
            if checker.IsValueCreated then
                checker.Value.InvalidateAll())

    member val FcsProjectProvider = Unchecked.defaultof<IFcsProjectProvider> with get, set
    member val AssemblyReaderShim = Unchecked.defaultof<IFcsAssemblyReaderShim> with get, set

    member x.Checker = checker.Value

    member this.AssertFcsAccessThread() =
        ()
        // if Shell.Instance.IsTestShell then () else
        //
        // if configurations.IsInternalMode() && Interruption.Current.IsEmpty then
        //     if locks.IsOnMainThread() then
        //         logger.Error("Accessing FCS without interruption (main thread)")
        //     else
        //         logger.Error("Accessing FCS without interruption")

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
        x.AssertFcsAccessThread()

        let psiModule = sourceFile.PsiModule
        // check if is active ...
        if useTransparentCompiler then ()
        match x.FcsProjectProvider.GetFcsProject(psiModule) with
        | None -> None
        | Some fcsProject ->

        let snapshot = fcsProject.ProjectSnapshot
        // if not (fcsProject.IsKnownFile(sourceFile)) && not options.UseScriptResolutionRules then None else

        x.FcsProjectProvider.PrepareAssemblyShim(psiModule)

        let path = sourceFile.GetLocation().FullPath
        let source = FcsCheckerService.getSourceText sourceFile.Document
        logger.Trace("ParseAndCheckFile: start {0}, {1}", path, opName)

        // todo: don't cancel the computation when file didn't change
        match x.Checker.ParseAndCheckDocument(path, snapshot, allowStaleResults, opName).RunAsTask() with
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

    member x.TryGetStaleCheckResults([<NotNull>] file: IPsiSourceFile, opName) : FSharpCheckFileResults option =
        match x.FcsProjectProvider.GetProjectSnapshot(file) with
        | None -> None
        | Some snapshot ->

        let path = file.GetLocation().FullPath
        logger.Trace("TryGetStaleCheckResults: start {0}, {1}", path, opName)

        // TODO: TryGetRecentCheckResultsForFile for Snapshot
        // https://github.com/dotnet/fsharp/pull/16720
        None
        // match x.Checker.TryGetRecentCheckResultsForFile(path, snapshot) with
        // | Some (_, checkResults, _) ->
        //     logger.Trace("TryGetStaleCheckResults: finish {0}, {1}", path, opName)
        //     Some checkResults
        //
        // | _ ->
        //     logger.Trace("TryGetStaleCheckResults: fail {0}, {1}", path, opName)
        //     None

    member x.GetCachedScriptSnapshot(path) =
        if checker.IsValueCreated then
            checker.Value.GetCachedScriptSnapshot(path)
        else None
    
    member x.InvalidateFcsProject(projectSnapshot: FSharpProjectSnapshot, invalidationType: FcsProjectInvalidationType) =
        if checker.IsValueCreated then
            match invalidationType with
            | FcsProjectInvalidationType.Invalidate ->
                logger.Trace("Remove FcsProject in FCS: {0}", projectSnapshot.ProjectFileName)
                checker.Value.ClearCache(Seq.singleton projectSnapshot.Identifier)
            | FcsProjectInvalidationType.Remove ->
                logger.Trace("Invalidate FcsProject in FCS: {0}", projectSnapshot.ProjectFileName)
                // See: https://github.com/dotnet/fsharp/pull/16718
                checker.Value.InvalidateConfiguration(projectSnapshot)

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

    abstract GetProjectSnapshot: sourceFile: IPsiSourceFile -> FSharpProjectSnapshot option
    abstract GetProjectSnapshot: psiModule: IPsiModule -> FSharpProjectSnapshot option

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
    abstract GetScriptSnapshot: IPsiSourceFile -> FSharpProjectSnapshot option
    // TODO: unused?
    // abstract GetScriptOptions: VirtualFileSystemPath * string -> FSharpProjectOptions option
    abstract SnapshotUpdated: Signal<VirtualFileSystemPath * FSharpProjectSnapshot>
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
