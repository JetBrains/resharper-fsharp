namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Features.ReSpeller
open JetBrains.ReSharper.Plugins.FSharp

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<DaemonEngineZone>
    interface IRequire<DaemonZone>
    interface IRequire<ILanguageFSharpZone>
    interface IRequire<IReSpellerZone>
    interface IRequire<ISinceClr4HostZone>
