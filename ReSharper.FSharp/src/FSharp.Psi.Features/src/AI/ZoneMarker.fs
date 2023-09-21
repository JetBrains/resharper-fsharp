namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AI

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel.NuGet
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.ReSharper.Feature.Services.AI

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IArtificialIntelligenceZone>
    interface IRequire<IHostSolutionZone>
    interface IRequire<INuGetZone>
