namespace JetBrains.ReSharper.Plugins.FSharp

open JetBrains.Application.BuildScript.Application.Zones

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<ILanguageFSharpZone>
