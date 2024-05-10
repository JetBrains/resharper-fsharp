namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System
open System.Collections.Concurrent
open System.Collections.Generic
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
open JetBrains.Threading
open JetBrains.Util

[<SolutionComponent>]
type ScriptFcsProjectProvider(lifetime: Lifetime, logger: ILogger, checkerService: FcsCheckerService,
        scriptSettings: FSharpScriptSettingsProvider, fsSourceCache: FSharpSourceCache, toolset: ISolutionToolset) =

    let defaultOptionsLock = obj()

    let scriptFcsProjects = ConcurrentDictionary<VirtualFileSystemPath, FcsProject option>()
    let requiringUpdate = ConcurrentDictionary<VirtualFileSystemPath, LifetimeDefinition>()

    let mutable defaultOptions: FSharpProjectOptions option option = None

    let optionsUpdated =
        new Signal<VirtualFileSystemPath * FSharpProjectOptions>("ScriptFcsProjectProvider.optionsUpdated")

    let isHeadless =
        let var = Environment.GetEnvironmentVariable("JET_HEADLESS_MODE") |> Option.ofObj |> Option.defaultValue "false"
        let parsed, isHeadless = bool.TryParse(var)
        parsed && isHeadless

    do
        fsSourceCache.FileUpdated.Advise(lifetime, fun (path: VirtualFileSystemPath) ->
            match path.ExtensionNoDot with
            | "fsx" | "fsscript" ->
                  requiringUpdate.AddOrUpdate(path,
                      (fun path -> null),
                      (fun path work -> (if isNull work then () else work.Terminate()); null)) |> ignore
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

    let getOptionsImpl (path: VirtualFileSystemPath) source =
        let path = path.FullPath
        let source = SourceText.ofString source
        let targetNetFramework = not PlatformUtil.IsRunningOnCore && scriptSettings.TargetNetFramework.Value

        let toolset = toolset.GetDotNetCoreToolset()
        let getScriptOptionsAsync =
            if isNotNull toolset && isNotNull toolset.Sdk then
                let sdkRootFolder = toolset.Cli.NotNull("cli").SdkRootFolder.NotNull("sdkRootFolder")
                let sdkFolderPath = sdkRootFolder / toolset.Sdk.NotNull("sdk").FolderName.NotNull("sdkFolderName")
                checkerService.Checker.GetProjectOptionsFromScript(path, source,
                    otherFlags = otherFlags.Value.Value,
                    assumeDotNetFramework = targetNetFramework,
                    sdkDirOverride = sdkFolderPath.FullPath)
            else
                checkerService.Checker.GetProjectOptionsFromScript(path, source,
                    otherFlags = otherFlags.Value.Value,
                    assumeDotNetFramework = targetNetFramework)

        try
            let options, errors = getScriptOptionsAsync.RunAsTask()
            if not errors.IsEmpty then
                logErrors logger (sprintf "Script options for %s" path) errors
            Some options
        with
        | OperationCanceled -> reraise()
        | exn ->
            logger.Warn("Error while getting script options for {0}: {1}", path, exn.Message)
            logger.LogExceptionSilently(exn)
            None

    let getDefaultOptions (path: VirtualFileSystemPath) =
        let withPath (options: FSharpProjectOptions option) =
            match options with
            | Some options -> Some { options with SourceFiles = [| path.FullPath |] }
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
            let parsingOptions = 
                { FSharpParsingOptions.Default with
                    SourceFiles = [| path.FullPath |]
                    ConditionalDefines = ImplicitDefines.scriptDefines
                    IsInteractive = true
                    IsExe = true }

            { OutputPath = path
              ProjectOptions = options
              ParsingOptions = parsingOptions
              FileIndices = dict [path, 0]
              ImplementationFilesWithSignatures = EmptySet.Instance
              ReferencedModules = EmptySet.Instance }
        )

    let rec updateOptionsIfNeeded path source oldOptionsExistence =
        let lifetimeDefinition = new LifetimeDefinition()
        if not (requiringUpdate.TryUpdate(path, lifetimeDefinition, null)) then () else
        let lifetime = lifetimeDefinition.Lifetime

        let updateOptionsAsync =
            task {
                if not lifetime.IsAlive then () else

                let newOptions = getOptionsImpl path source //TODO: do not block?
                let oldOptions = oldOptionsExistence |> Option.bind id

                let fcsProject =
                    newOptions //TODO: use createFcsProject?
                    |> Option.map (fun options ->
                        let parsingOptions =
                            { FSharpParsingOptions.Default with
                                SourceFiles = [| path.FullPath |]
                                ConditionalDefines = ImplicitDefines.scriptDefines
                                IsInteractive = true
                                IsExe = true }

                        let indices = Dictionary()

                        { OutputPath = path
                          ProjectOptions = options
                          ParsingOptions = parsingOptions
                          FileIndices = indices
                          ImplementationFilesWithSignatures = EmptySet.Instance
                          ReferencedModules = EmptySet.Instance }
                    )

                if not (lifetime.TryExecute(fun () -> scriptFcsProjects[path] <- fcsProject).Succeed) then () else

                match oldOptions, newOptions with
                | Some oldOptions, Some newOptions ->
                    let areEqualForChecking (options1: FSharpProjectOptions) (options2: FSharpProjectOptions) =
                        let arrayEq a1 a2 =
                            Array.length a1 = Array.length a2 && Array.forall2 (=) a1 a2

                        arrayEq options1.OtherOptions options2.OtherOptions &&
                        arrayEq options1.SourceFiles options2.SourceFiles

                    if not (areEqualForChecking oldOptions.ProjectOptions newOptions) then
                        optionsUpdated.Fire((path, newOptions))

                | _, Some newOptions ->
                    optionsUpdated.Fire((path, newOptions))

                | _ -> ()
            }

        if isHeadless then
            updateOptionsAsync.GetAwaiter().GetResult()
        else
            updateOptionsAsync.NoAwait()

    let rec getFcsProject path source allowRetry : FcsProject option =
        match tryGetValue path scriptFcsProjects with
        | Some fcsProject as scriptFcsProject ->
            updateOptionsIfNeeded path source scriptFcsProject
            fcsProject
        | _ ->
            requiringUpdate.TryAdd(path, null) |> ignore
            updateOptionsIfNeeded path source None

            if isHeadless && allowRetry then
                getFcsProject path source false
            else
                getDefaultOptions path |> createFcsProject path

    let getOptions path source : FSharpProjectOptions option =
        getFcsProject path source true |> Option.map (fun fcsProject -> fcsProject.ProjectOptions)

    interface IScriptFcsProjectProvider with
        member x.GetScriptOptions(path: VirtualFileSystemPath, source) =
            getOptions path source

        member x.GetScriptOptions(file: IPsiSourceFile) =
            let path = file.GetLocation()
            let source = file.Document.GetText()
            getOptions path source

        member this.GetFcsProject(sourceFile) =
            let path = sourceFile.GetLocation()
            let source = sourceFile.Document.GetText()
            getFcsProject path source true

        member this.OptionsUpdated = optionsUpdated
        member this.SyncUpdate = isHeadless
