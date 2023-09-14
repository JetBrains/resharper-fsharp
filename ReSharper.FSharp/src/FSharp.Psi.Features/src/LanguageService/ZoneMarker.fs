namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Platform.RdFramework
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Feature.Services.Breadcrumbs
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.Rider.Backend.Env
open JetBrains.Rider.Backend.Product

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<DaemonZone>
    interface IRequire<IBreadcrumbsZone>
    interface IRequire<ICodeEditingZone>
    interface IRequire<IRdFrameworkZone>
    interface IRequire<IRiderFeatureZone>
    interface IRequire<IRiderFeatureEnvironmentZone>
    interface IRequire<IRiderProductEnvironmentZone>
    interface IRequire<ISinceClr4HostZone>
