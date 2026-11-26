namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.UnitTestFramework

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IUnitTestingZone>
