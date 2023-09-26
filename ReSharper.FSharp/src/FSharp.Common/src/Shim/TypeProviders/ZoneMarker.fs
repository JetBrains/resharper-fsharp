namespace  JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel
open JetBrains.ProjectModel.NuGet

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IProjectModelZone>
    interface IRequire<INuGetZone>
