namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.TextControl

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<ITextControlsZone>
