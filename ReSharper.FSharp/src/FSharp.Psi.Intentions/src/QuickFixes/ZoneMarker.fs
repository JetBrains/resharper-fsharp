namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Resources.Shell

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<PsiFeaturesImplZone>
