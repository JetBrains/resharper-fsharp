#nowarn FS0057

namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System
open System.Collections.Concurrent
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.CodeAnalysis.ProjectSnapshot
open JetBrains.Application.Parts
open JetBrains.Application.Threading
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.Threading
open JetBrains.Util

[<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
type ScriptFcsProjectProvider(lifetime: Lifetime, logger: ILogger, checkerService: FcsCheckerService,
        scriptSettings: FSharpScriptSettingsProvider, toolset: ISolutionToolset, locks: IShellLocks) =

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
            let config, errors = checkerService.GetProjectConfigFromScript(path, source, otherFlags.Value.Value, targetNetFramework, sdkDirOverride)
            if not errors.IsEmpty then logErrors logger $"Script options for %s{path}" errors
            Some config
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
                Some (FcsProjectOptions.FcsProjectSnapshot(snapshot.Replace([fileSnapshot])))

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

    let createFcsProject (path: VirtualFileSystemPath) config =
        config
        |> Option.map (fun config ->
            { OutputPath = path
              Options = config
              FileIndices = dict [path, 0]
              ImplementationFilesWithSignatures = EmptySet.Instance
              ReferencedModules = EmptySet.Instance }
        )

    let rec updateOptionsIfNeeded path source =
        let sequentialLifetimes = scriptsUpdateLifetimes.GetOrAdd(path, SequentialLifetimes(lifetime))
        let currentLifetime = sequentialLifetimes.Next()

        locks.StartReadActionAsync(currentLifetime, Action(fun _ ->
            if not currentLifetime.IsAlive then () else

            let newOptions = getOptionsImpl path source
            let oldOptions = tryGetValue path scriptFcsProjects |> Option.bind id |> Option.map _.Options

            if not currentLifetime.IsAlive then () else

            let inline update path newOptions =
                let fcsProject = createFcsProject path (Some newOptions)
                if not (currentLifetime.TryExecute(fun () -> scriptFcsProjects[path] <- fcsProject).Succeed) then ()
                else optionsUpdated.Fire((path, newOptions))

            match oldOptions, newOptions with
            | Some oldOptions, Some newOptions when not (oldOptions.AreSameForChecking(newOptions)) ->
                update path newOptions

            | _, Some newOptions -> update path newOptions
            | _ -> ()
        )).NoAwait()


    let rec getFcsProject path source allowRetry : FcsProject option =
        updateOptionsIfNeeded path source

        match tryGetValue path scriptFcsProjects with
        | Some fcsProject -> fcsProject
        | _ ->
            if isHeadless && allowRetry then
                getFcsProject path source false
            else
                getDefaultOptions path |> createFcsProject path

    interface IScriptFcsProjectProvider with
        member this.GetFcsProject(sourceFile) =
            let path = sourceFile.GetLocation()
            let source = sourceFile.Document.GetText()
            getFcsProject path source true

        member this.OptionsUpdated = optionsUpdated
        member this.SyncUpdate = isHeadless
