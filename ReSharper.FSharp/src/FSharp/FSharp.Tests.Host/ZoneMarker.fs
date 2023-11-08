namespace JetBrains.ReSharper.Plugins.FSharp.TestsHost

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Rider.Backend.Env

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IRiderCoreTestingZone>