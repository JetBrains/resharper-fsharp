namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Platform.RdFramework
open JetBrains.ProjectModel.NuGet
open JetBrains.ProjectModel.ProjectsHost.SolutionHost

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IHostSolutionZone>
    interface IRequire<INuGetZone>
    interface IRequire<IRdFrameworkZone>
