namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Platform.RdFramework
open JetBrains.ProjectModel.NuGet
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.ReSharper.Plugins.FSharp

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IHostSolutionZone>
    interface IRequire<ILanguageFSharpZone>
    interface IRequire<INuGetZone>
    interface IRequire<IRdFrameworkZone>
    interface IRequire<ISinceClr4HostZone>
