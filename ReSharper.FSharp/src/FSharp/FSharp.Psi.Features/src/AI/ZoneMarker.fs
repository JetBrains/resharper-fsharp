namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AI

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Feature.Services.AI

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IArtificialIntelligenceZone>
