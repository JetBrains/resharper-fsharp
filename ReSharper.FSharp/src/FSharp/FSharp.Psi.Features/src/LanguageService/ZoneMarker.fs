namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Plugins.FSharp

[<ZoneMarker(typeof<IFSharpPsiZone>)>]
type ZoneMarker() = class end
