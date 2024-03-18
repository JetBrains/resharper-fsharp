namespace JetBrains.ReSharper.Plugins.FSharp.Checker

#nowarn "57"

open System
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.Util

[<SolutionComponent>]
type ScriptFcsProjectProvider(lifetime: Lifetime, logger: ILogger, checkerService: FcsCheckerService,
        scriptSettings: FSharpScriptSettingsProvider, fsSourceCache: FSharpSourceCache, toolset: ISolutionToolset) =

    let defaultOptionsLock = obj()

    let scriptFcsProjects = Dictionary<VirtualFileSystemPath, FcsProject option>()

    let mutable defaultSnapshot: FSharpProjectSnapshot option option = None

    let currentRequests = HashSet<VirtualFileSystemPath>()
    let dirtyPaths = HashSet<VirtualFileSystemPath>()

    let snapshotUpdated =
        new Signal<VirtualFileSystemPath * FSharpProjectSnapshot>("ScriptFcsProjectProvider.optionsUpdated")

    let isHeadless =
        let var = Environment.GetEnvironmentVariable("JET_HEADLESS_MODE") |> Option.ofObj |> Option.defaultValue "false"
        let parsed, isHeadless = bool.TryParse(var)
        parsed && isHeadless

    do
        fsSourceCache.FileUpdated.Advise(lifetime, fun (path: VirtualFileSystemPath) ->
            match path.ExtensionNoDot with
            | "fsx" | "fsscript" -> dirtyPaths.Add(path) |> ignore
            | _ -> ())

    let defaultFlags =
        [| "--warnon:1182"

           if PlatformUtil.IsRunningOnCore then
               "--targetprofile:netcore"
               "--simpleresolution" |]

    let getOtherFlags languageVersion =
        if languageVersion = FSharpLanguageVersion.Default then defaultFlags else

        let languageVersionOptionArg = FSharpLanguageVersion.toCompilerArg languageVersion
        Array.append defaultFlags [| languageVersionOptionArg |]

    let otherFlags =
        lazy
            let languageVersion = scriptSettings.LanguageVersion
            let flags = new Property<_>("FSharpScriptOtherFlags", getOtherFlags languageVersion.Value)
            IPropertyEx.FlowInto(languageVersion, lifetime, flags, getOtherFlags)
            flags

    let getSnapshotImpl (path: VirtualFileSystemPath) source : FSharpProjectSnapshot option =
        let path = path.FullPath
        let source = SourceTextNew.ofString source
        let targetNetFramework = not PlatformUtil.IsRunningOnCore && scriptSettings.TargetNetFramework.Value

        let toolset = toolset.GetDotNetCoreToolset()
        let getScriptSnapshotAsync: Async<FSharpProjectSnapshot * FSharp.Compiler.Diagnostics.FSharpDiagnostic list> =
            if isNotNull toolset && isNotNull toolset.Sdk then
                let sdkRootFolder = toolset.Cli.NotNull("cli").SdkRootFolder.NotNull("sdkRootFolder")
                let sdkFolderPath = sdkRootFolder / toolset.Sdk.NotNull("sdk").FolderName.NotNull("sdkFolderName")
                checkerService.Checker.GetProjectSnapshotFromScript(path, source,
                    otherFlags = otherFlags.Value.Value,
                    assumeDotNetFramework = targetNetFramework,
                    sdkDirOverride = sdkFolderPath.FullPath)
            else
                checkerService.Checker.GetProjectSnapshotFromScript(path, source,
                    otherFlags = otherFlags.Value.Value,
                    assumeDotNetFramework = targetNetFramework)

        try
            let snapshot, errors = getScriptSnapshotAsync.RunAsTask()
            if not errors.IsEmpty then
                logErrors logger (sprintf "Script options for %s" path) errors
            Some snapshot
        with
        | OperationCanceled -> reraise()
        | exn ->
            logger.Warn("Error while getting script options for {0}: {1}", path, exn.Message)
            logger.LogExceptionSilently(exn)
            None

    let getDefaultSnapshot (path: VirtualFileSystemPath) (source: string): ProjectSnapshot.FSharpProjectSnapshot option =
        let withPath (snapshot: FSharpProjectSnapshot option) =
            snapshot
            |> Option.map (fun snapshot ->
                let name = path.Name
                let version = string path.Info.ModificationTimeUtc.Ticks
                let getSource () = SourceTextNew.ofString source |> Task.FromResult
                let sourceFiles =
                    ProjectSnapshot.FSharpFileSnapshot.Create(name, version, getSource)
                    |> List.singleton
                
                FSharpProjectSnapshot.Create(
                    snapshot.ProjectFileName,
                    snapshot.ProjectId,
                    sourceFiles,
                    snapshot.ReferencesOnDisk,
                    snapshot.OtherOptions,
                    snapshot.ReferencedProjects,
                    snapshot.IsIncompleteTypeCheckEnvironment,
                    snapshot.UseScriptResolutionRules,
                    snapshot.LoadTime,
                    snapshot.UnresolvedReferences,
                    snapshot.OriginalLoadReferences,
                    snapshot.Stamp
                )
            )

        match defaultSnapshot with
        | Some options -> withPath options
        | _ ->

        lock defaultOptionsLock (fun _ ->
            match defaultSnapshot with
            | Some options -> withPath options
            | _ ->

            let newOptions = getSnapshotImpl path ""
            defaultSnapshot <- Some newOptions
            newOptions
        )

    let createFcsProject (path: VirtualFileSystemPath) snapshot =
        snapshot
        |> Option.map (fun snapshot ->
            let parsingOptions = 
                { FSharpParsingOptions.Default with
                    SourceFiles = [| path.FullPath |]
                    ConditionalDefines = ImplicitDefines.scriptDefines
                    IsInteractive = true
                    IsExe = true }

            { OutputPath = path
              ProjectSnapshot = snapshot
              ParsingOptions = parsingOptions
              FileIndices = dict [path, 0]
              ImplementationFilesWithSignatures = EmptySet.Instance
              ReferencedModules = EmptySet.Instance }
        )

    let rec updateOptions path source =
        if currentRequests.Contains(path) then () else

        let updateOptionsAsync = 
            async {
                if currentRequests.Contains(path) then () else

                try
                    currentRequests.Add(path) |> ignore
                    dirtyPaths.Remove(path) |> ignore
                    let oldOptions = tryGetValue path scriptFcsProjects |> Option.bind id
                    let newSnapshot = getSnapshotImpl path source

                    scriptFcsProjects[path] <-
                        newSnapshot
                        |> Option.map (fun snapshot ->
                            let parsingOptions = 
                                { FSharpParsingOptions.Default with
                                    SourceFiles = [| path.FullPath |]
                                    ConditionalDefines = ImplicitDefines.scriptDefines
                                    IsInteractive = true
                                    IsExe = true }

                            let indices = Dictionary()

                            { OutputPath = path
                              ProjectSnapshot = snapshot
                              ParsingOptions = parsingOptions
                              FileIndices = indices
                              ImplementationFilesWithSignatures = EmptySet.Instance
                              ReferencedModules = EmptySet.Instance }
                        )

                    match oldOptions, newSnapshot with
                    | Some oldOptions, Some newSnapshot ->
                        let areEqualForChecking (options1: FSharpProjectSnapshot) (options2: FSharpProjectSnapshot) =
                            let listEq l1 l2 =
                                List.length l1 = List.length l2 && List.forall2 (=) l1 l2

                            listEq options1.OtherOptions options2.OtherOptions &&
                            listEq options1.ReferencesOnDisk options2.ReferencesOnDisk &&
                            listEq options1.SourceFiles options2.SourceFiles

                        if not (areEqualForChecking oldOptions.ProjectSnapshot newSnapshot) then
                            snapshotUpdated.Fire((path, newSnapshot))

                    | _, Some newOptions ->
                        snapshotUpdated.Fire((path, newOptions))

                    | _ -> ()

                finally
                    currentRequests.Remove(path) |> ignore
            }

        if isHeadless then
            Async.RunSynchronously updateOptionsAsync
        else
            Async.Start updateOptionsAsync

    let rec getFcsProject path source allowRetry : FcsProject option =
        match tryGetValue path scriptFcsProjects with
        | Some fcsProject ->
            if dirtyPaths.Contains(path) then
                updateOptions path source
            fcsProject
        | _ ->
            updateOptions path source

            if isHeadless && allowRetry then
                getFcsProject path source false
            else
                getDefaultSnapshot path source |> createFcsProject path

    let getOptions path source : FSharpProjectSnapshot option =
        getFcsProject path source true |> Option.map (fun fcsProject -> fcsProject.ProjectSnapshot)

    interface IScriptFcsProjectProvider with
        // TODO: unused ?
        // member x.GetScriptOptions(path: VirtualFileSystemPath, source) =
        //     getOptions path source

        member x.GetScriptSnapshot(file: IPsiSourceFile) =
            let path = file.GetLocation()
            let source = file.Document.GetText()
            getOptions path source

        member this.GetFcsProject(sourceFile) =
            let path = sourceFile.GetLocation()
            let source = sourceFile.Document.GetText()
            getFcsProject path source true

        member this.SnapshotUpdated = snapshotUpdated
        member this.SyncUpdate = isHeadless
