namespace  JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Rider.Backend.Product

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IRiderProductEnvironmentZone>
