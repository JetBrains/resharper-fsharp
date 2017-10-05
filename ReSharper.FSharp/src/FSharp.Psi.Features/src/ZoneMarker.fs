namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Plugins.FSharp.Psi

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<ILanguageFSharpZone>