namespace  JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel
open JetBrains.ProjectModel.NuGet
open JetBrains.Rider.Model

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<INuGetZone>
    interface IRequire<IProjectModelZone>
    interface IRequire<IRiderModelZone>
