namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Daemon.Syntax
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Resources.Shell

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<DaemonZone>
    interface IRequire<ILanguageFSharpZone>
    interface IRequire<ISyntaxHighlightingZone>
    interface IRequire<PsiFeaturesImplZone>
