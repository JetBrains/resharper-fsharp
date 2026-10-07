namespace JetBrains.ReSharper.Plugins.FSharp

open JetBrains.Application.BuildScript.Application.Zones

[<ZoneMarker(typeof<IFSharpPsiZone>)>]
type ZoneMarker() = class end
