namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System
open System.Collections.Concurrent
open System.Collections.Generic
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.CodeAnalysis.ProjectSnapshot
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Parts
open JetBrains.Application.Threading
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.Threading
open JetBrains.Util

[<ZoneMarker(typeof<IFSharpPluginZone>)>]
[<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
type ScriptFcsProjectProvider(lifetime: Lifetime, logger: ILogger, checkerService: FcsCheckerService,
        scriptSettings: FSharpScriptSettingsProvider, toolset: ISolutionToolset, locks: IShellLocks,
        sourceCache: FSharpSourceCache) =

    let defaultOptionsLock = obj()

    let scriptFcsProjects = ConcurrentDictionary<VirtualFileSystemPath, FcsProject option>()
    let scriptsUpdateLifetimes = ConcurrentDictionary<VirtualFileSystemPath, SequentialLifetimes>()

    let mutable defaultOptions: FcsProjectOptions option option = None

    let optionsUpdated =
        new Signal<VirtualFileSystemPath * FcsProjectOptions>("ScriptFcsProjectProvider.optionsUpdated")

    let isHeadless =
        let var = Environment.GetEnvironmentVariable("JET_HEADLESS_MODE") |> Option.ofObj |> Option.defaultValue "false"
        let parsed, isHeadless = bool.TryParse(var)
        parsed && isHeadless

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

    let getOptionsImpl (path: VirtualFileSystemPath) source =
        let path = path.FullPath
        let targetNetFramework = not PlatformUtil.IsRunningOnCore && scriptSettings.TargetNetFramework.Value

        let toolset = toolset.GetDotNetCoreToolset()
        let sdkDirOverride =
            if isNull toolset || isNull toolset.Sdk then None else

            let sdkRootFolder = toolset.Cli.NotNull("cli").SdkRootFolder.NotNull("sdkRootFolder")
            let sdkFolderPath = sdkRootFolder / toolset.Sdk.NotNull("sdk").FolderName.NotNull("sdkFolderName")
            Some sdkFolderPath.FullPath

        try
            let options, errors = checkerService.GetProjectOptionsFromScript(path, source, otherFlags.Value.Value, targetNetFramework, sdkDirOverride)
            if not errors.IsEmpty then logErrors logger $"Script options for %s{path}" errors
            Some options
        with
        | OperationCanceled -> reraise()
        | exn ->
            logger.Warn("Error while getting script options for {0}: {1}", path, exn.Message)
            logger.LogExceptionSilently(exn)
            None

    let getDefaultOptions (path: VirtualFileSystemPath) =
        let withPath (options: FcsProjectOptions option) =
            match options with
            | Some (FcsProjectOptions.FcsProjectOptions(options, parsingOptions)) ->
                Some (FcsProjectOptions.FcsProjectOptions(
                          { options with SourceFiles = [| path.FullPath |] },
                          { parsingOptions with SourceFiles = [| path.FullPath |] }))

            | Some (FcsProjectOptions.FcsProjectSnapshot(snapshot)) ->
                let fileSnapshot = FSharpFileSnapshot.CreateFromFileSystem(path.FullPath)
                Some (FcsProjectOptions.FcsProjectSnapshot(snapshot.With(sourceFiles = [fileSnapshot])))

            | _ -> None

        match defaultOptions with
        | Some options -> withPath options
        | _ ->

        lock defaultOptionsLock (fun _ ->
            match defaultOptions with
            | Some options -> withPath options
            | _ ->

            let newOptions = getOptionsImpl path ""
            defaultOptions <- Some newOptions
            newOptions
        )

    let createFcsProject (path: VirtualFileSystemPath) options =
        options
        |> Option.map (fun options ->
            let fileIndices =
                match options with
                | FcsProjectOptions _ -> dict [path, 0]
                | FcsProjectSnapshot projectSnapshot ->
                    projectSnapshot.SourceFiles
                    |> Seq.mapi (fun i fileSnapshot -> VirtualFileSystemPath.TryParse(fileSnapshot.FileName, InteractionContext.SolutionContext), i)
                    |> dict

            { OutputPath = path
              Options = options
              FileIndices = fileIndices
              ImplementationFilesWithSignatures = EmptySet.Instance
              ReferencedModules = EmptySet.Instance }
        )

    let changedFiles = HashSet<VirtualFileSystemPath>()

    let updateFile (path: VirtualFileSystemPath) =
        for KeyValue(scriptPath, fcsProject) in scriptFcsProjects do
            match fcsProject with
            | Some ({ Options = FcsProjectOptions.FcsProjectSnapshot _ } as fcsProject) when fcsProject.FileIndices.ContainsKey(path) ->
                scriptFcsProjects[scriptPath] <- Some (fcsProject.WithUpdatedFile(path))
            | _ -> ()

    let processChangedFiles () =
        if changedFiles.Count = 0 then () else

        lock changedFiles (fun _ ->
            for path in changedFiles do
                updateFile path

            changedFiles.Clear()
        )

    do
        sourceCache.FileUpdated.Advise(lifetime, fun path ->
            if checkerService.UseTransparentCompiler then
                lock changedFiles (fun _ -> changedFiles.Add(path) |> ignore)
        )

    let rec updateOptionsIfNeeded path (document: IDocument) =
        processChangedFiles ()

        let sequentialLifetimes = scriptsUpdateLifetimes.GetOrAdd(path, SequentialLifetimes(lifetime))
        let currentLifetime = sequentialLifetimes.Next()

        locks.StartReadActionAsync(currentLifetime, Action(fun _ ->
            if not currentLifetime.IsAlive then () else

            let newOptions = getOptionsImpl path (document.GetText())
            let oldOptions = tryGetValue path scriptFcsProjects |> Option.bind id |> Option.map _.Options

            if not currentLifetime.IsAlive then () else

            let inline update path newOptions =
                let fcsProject = createFcsProject path (Some newOptions)
                if not (currentLifetime.TryExecute(fun () -> lock changedFiles (fun _ -> scriptFcsProjects[path] <- fcsProject)).Succeed) then ()
                else optionsUpdated.Fire((path, newOptions))

            match oldOptions, newOptions with
            | Some oldOptions, Some newOptions ->
                if not (oldOptions.AreSameForChecking(newOptions)) then
                    update path newOptions

            | _, Some newOptions -> update path newOptions
            | _ -> ()
        )).NoAwait()


    let rec getFcsProject (sourceFile: IPsiSourceFile) allowRetry : FcsProject option =
        let path = sourceFile.GetLocation()
        let document = sourceFile.Document

        updateOptionsIfNeeded path document

        match tryGetValue path scriptFcsProjects with
        | Some fcsProject -> fcsProject
        | _ ->
            if isHeadless && allowRetry then
                getFcsProject sourceFile false
            else
                getDefaultOptions path |> createFcsProject path

    interface IScriptFcsProjectProvider with
        member this.GetFcsProject(sourceFile) =
            getFcsProject sourceFile true

        member this.OptionsUpdated = optionsUpdated
        member this.SyncUpdate = isHeadless
