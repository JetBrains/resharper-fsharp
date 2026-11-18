namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Build

open System
open System.Collections.Concurrent
open System.IO
open System.Text
open JetBrains.Application.Parts
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.MSBuild.Extensibility
open JetBrains.ProjectModel.Properties
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.Util
open Newtonsoft.Json.Linq

module FscRuntimeConfig =
    let [<Literal>] TieredPgo = "System.Runtime.TieredPGO"
    let [<Literal>] CallCountingDelayMs = "System.Runtime.TieredCompilation.CallCountingDelayMs"

    let tryReduceJitWarmup (runtimeConfig: string) : string option =
        let json = JObject.Parse(runtimeConfig)

        match json.SelectToken("runtimeOptions.configProperties") with
        | :? JObject as configProperties when
                isNull (configProperties.Property(TieredPgo)) && isNull (configProperties.Property(CallCountingDelayMs)) ->
            configProperties.Add(TieredPgo, JValue(false))
            configProperties.Add(CallCountingDelayMs, JValue(0))
            Some(json.ToString())
        | _ -> None


[<SolutionInstanceComponent(Instantiation.DemandAnyThreadSafe)>]
type FSharpBuildPropertiesProvider(lifetime: Lifetime, optionsProvider: FSharpOptionsProvider, logger: ILogger) =
    let tempRuntimeConfigPaths = ConcurrentDictionary<VirtualFileSystemPath, VirtualFileSystemPath>()

    let tryWriteTempRuntimeConfig (fscPath: VirtualFileSystemPath) (runtimeConfig: string) =
        try
            let path =
                tempRuntimeConfigPaths.GetOrAdd(fscPath, fun _ ->
                    VirtualFileSystemDefinition.CreateTemporaryFile(
                        lifetime, fscPath.InteractionContext, extensionWithDot = ".runtimeconfig.json", prefix = "fsc"))

            if not path.ExistsFile || path.ReadAllText2().Text <> runtimeConfig then
                path.WriteAllText(runtimeConfig, UTF8Encoding(false))

            Some path
        with :? IOException | :? UnauthorizedAccessException as exn ->
            logger.Warn("Could not write the F# compiler runtime config: {0}", exn.Message)
            None

    interface IProjectBuildAdditionalPropertiesProvider with
        member x.TryGetProjectProperties(project, isConsoleBuild) =
            if isConsoleBuild || not optionsProvider.ReduceCompilerJitWarmup.Value then null else

            let targetFrameworkId = project.GetCurrentTargetFrameworkId()
            let configuration = project.ProjectProperties.TryGetConfiguration<IFSharpProjectConfiguration>(targetFrameworkId)

            let fscPath = getFscPath configuration
            if not fscPath.IsAbsolute then null else

            let fscRuntimeConfigPath = fscPath.ChangeExtension("runtimeconfig.json")
            if not fscRuntimeConfigPath.ExistsFile then null else

            match FscRuntimeConfig.tryReduceJitWarmup (fscRuntimeConfigPath.ReadAllText2().Text) with
            | None -> null
            | Some runtimeConfig ->

            match tryWriteTempRuntimeConfig fscPath runtimeConfig with
            | None -> null
            | Some tempRuntimeConfigPath ->

            let fscCommand = $"exec --runtimeconfig \"{tempRuntimeConfigPath.FullPath}\" \"{fscPath.FullPath}\""
            readOnlyDict [ FSharpProperties.DotnetFscCompilerPath, fscCommand ]
