namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ExternalSources

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Plugins.FSharp

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<JetBrains.ReSharper.Feature.Services.ExternalSources.ExternalSourcesZone>
