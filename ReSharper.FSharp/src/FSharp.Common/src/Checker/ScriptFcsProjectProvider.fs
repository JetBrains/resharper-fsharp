namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System.Collections.Generic
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open JetBrains.DataFlow
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
        scriptSettings: FSharpScriptSettingsProvider, fsSourceCache: FSharpSourceCache) =

    let defaultOptionsLock = obj()

    let scriptOptions = Dictionary<VirtualFileSystemPath, FSharpProjectOptions option>()

    let mutable defaultOptions: FSharpProjectOptions option option = None

    let currentRequests = HashSet<VirtualFileSystemPath>()
    let dirtyPaths = HashSet<VirtualFileSystemPath>()

    let optionsUpdated =
        new Signal<VirtualFileSystemPath * FSharpProjectOptions>(lifetime, "ScriptFcsProjectProvider.optionsUpdated")

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
            IPropertyEx.FlowInto(languageVersion, lifetime, flags, fun version -> getOtherFlags version)
            flags

    let getOptionsImpl (path: VirtualFileSystemPath) source =
        let path = path.FullPath
        let source = SourceText.ofString source
        let getScriptOptionsAsync =
            let targetNetFramework = not PlatformUtil.IsRunningOnCore && scriptSettings.TargetNetFramework.Value
            checkerService.Checker.GetProjectOptionsFromScript(path, source,
                otherFlags = otherFlags.Value.Value, assumeDotNetFramework = targetNetFramework)
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

    let rec updateOptions path source =
        if currentRequests.Contains(path) then () else

        async {
            if currentRequests.Contains(path) then () else

            try
                currentRequests.Add(path) |> ignore
                dirtyPaths.Remove(path) |> ignore
                let oldOptions = tryGetValue path scriptOptions |> Option.bind id
                let newOptions = getOptionsImpl path source
                scriptOptions[path] <- newOptions

                match oldOptions, newOptions with
                | Some oldOptions, Some newOptions ->
                    let areEqualForChecking (options1: FSharpProjectOptions) (options2: FSharpProjectOptions) =
                        let arrayEq a1 a2 =
                            Array.length a1 = Array.length a2 && Array.forall2 (=) a1 a2

                        arrayEq options1.OtherOptions options2.OtherOptions &&
                        arrayEq options1.SourceFiles options2.SourceFiles

                    if not (areEqualForChecking oldOptions newOptions) then
                        optionsUpdated.Fire((path, newOptions))

                | _, Some newOptions ->
                    optionsUpdated.Fire((path, newOptions))

                | _ -> ()

            finally
                currentRequests.Remove(path) |> ignore
        } |> Async.Start

    let getOptions path source =
        match tryGetValue path scriptOptions with
        | Some options ->
            if dirtyPaths.Contains(path) then
                updateOptions path source
            options
        | _ ->
            updateOptions path source
            getDefaultOptions path

    interface IScriptFcsProjectProvider with
        member x.GetScriptOptions(path: VirtualFileSystemPath, source) =
            getOptions path source

        member x.GetScriptOptions(file: IPsiSourceFile) =
            let path = file.GetLocation()
            let source = file.Document.GetText()
            getOptions path source

        member this.OptionsUpdated = optionsUpdated
