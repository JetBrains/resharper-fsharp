namespace JetBrains.ReSharper.Plugins.FSharp

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Plugins.FSharp

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<ILanguageFSharpZone>
