namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Platform.RdFramework
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Feature.Services.Breadcrumbs
open JetBrains.ReSharper.Feature.Services.Daemon

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<DaemonZone>
    interface IRequire<IBreadcrumbsZone>
    interface IRequire<ICodeEditingZone>
    interface IRequire<IRdFrameworkZone>
    interface IRequire<ISinceClr4HostZone>
