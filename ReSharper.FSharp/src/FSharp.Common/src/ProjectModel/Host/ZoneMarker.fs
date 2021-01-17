namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel.ProjectsHost.SolutionHost

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IHostSolutionZone>
