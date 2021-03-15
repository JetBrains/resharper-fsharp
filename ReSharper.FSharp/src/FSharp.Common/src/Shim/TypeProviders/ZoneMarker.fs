namespace  JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Host.Product

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IRiderProductEnvironmentZone>
