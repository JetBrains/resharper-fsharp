namespace JetBrains.ReSharper.Plugins.FSharp.Settings

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.Rider.Model

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<ILanguageFSharpZone>
    interface IRequire<IRiderModelZone>
