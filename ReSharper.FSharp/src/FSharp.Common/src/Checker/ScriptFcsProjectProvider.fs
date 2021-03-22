namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open FSharp.Compiler.Text
open JetBrains.DataFlow
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.Util

[<SolutionComponent>]
type ScriptFcsProjectProvider(lifetime: Lifetime, logger: ILogger, checkerService: FcsCheckerService,
        scriptSettings: FSharpScriptSettingsProvider) =

    let getScriptOptionsLock = obj()

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

    let getOptions (path: FileSystemPath) source =
        let path = path.FullPath
        let source = SourceText.ofString source
        lock getScriptOptionsLock (fun _ ->
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
                None)

    interface IScriptFcsProjectProvider with
        member x.GetScriptOptions(path: FileSystemPath, source) =
            getOptions path source

        member x.GetScriptOptions(file: IPsiSourceFile) =
            let path = file.GetLocation()
            let source = file.Document.GetText()
            getOptions path source
