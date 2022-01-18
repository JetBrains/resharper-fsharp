namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Tooltips

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.RdBackend.Common.Env

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IResharperHostCoreFeatureZone>
