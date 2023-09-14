namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AI

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel.NuGet
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Feature.Services.AI
open JetBrains.Rider.Backend.Env

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IArtificialIntelligenceZone>
    interface IRequire<IHostSolutionZone>
    interface IRequire<INuGetZone>
    interface IRequire<IResharperHostCoreFeatureZone>
    interface IRequire<IRiderFeatureEnvironmentZone>
