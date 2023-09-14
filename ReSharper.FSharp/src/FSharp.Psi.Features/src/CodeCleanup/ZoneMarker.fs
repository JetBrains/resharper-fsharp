namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Platform.RdFramework
open JetBrains.ProjectModel.NuGet
open JetBrains.Rider.Backend.Env
open JetBrains.Rider.Backend.Product

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<INuGetZone>
    interface IRequire<IRdFrameworkZone>
    interface IRequire<IRiderFeatureZone>
    interface IRequire<IRiderProductEnvironmentZone>
    interface IRequire<ISinceClr4HostZone>
