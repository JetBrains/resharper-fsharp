namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Platform.RdFramework

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IRdFrameworkZone>
    interface IRequire<ISinceClr4HostZone>
