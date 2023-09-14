namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Features.ReSpeller
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.Rider.Backend.Env

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<DaemonEngineZone>
    interface IRequire<DaemonZone>
    interface IRequire<ILanguageFSharpZone>
    interface IRequire<IResharperHostCoreFeatureZone>
    interface IRequire<IReSpellerZone>
    interface IRequire<IRiderFeatureEnvironmentZone>
    interface IRequire<ISinceClr4HostZone>
