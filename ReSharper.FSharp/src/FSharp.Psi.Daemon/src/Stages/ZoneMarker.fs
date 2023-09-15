namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Daemon.Syntax

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<ISyntaxHighlightingZone>
