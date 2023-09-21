namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Platform.RdFramework
open JetBrains.ProjectModel.NuGet

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<INuGetZone>
    interface IRequire<IRdFrameworkZone>
    interface IRequire<ISinceClr4HostZone>
