namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Platform.RdFramework
open JetBrains.ProjectModel.NuGet
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.Rider.Backend.Env

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IHostSolutionZone>
    interface IRequire<ILanguageFSharpZone>
    interface IRequire<INuGetZone>
    interface IRequire<IRdFrameworkZone>
    interface IRequire<IResharperHostCoreFeatureZone>
    interface IRequire<IRiderFeatureEnvironmentZone>
    interface IRequire<ISinceClr4HostZone>
