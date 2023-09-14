namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Daemon.Syntax
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Rider.Backend.Env

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<DaemonZone>
    interface IRequire<ILanguageFSharpZone>
    interface IRequire<IResharperHostCoreFeatureZone>
    interface IRequire<IRiderFeatureEnvironmentZone>
    interface IRequire<ISyntaxHighlightingZone>
    interface IRequire<PsiFeaturesImplZone>
