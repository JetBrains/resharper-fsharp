namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Platform.RdFramework
open JetBrains.RdBackend.Common.Env
open JetBrains.Rider.Backend.Env
open JetBrains.Rider.Backend.Product

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IRdFrameworkZone>
    interface IRequire<IResharperHostCoreFeatureZone>
    interface IRequire<IRiderFeatureEnvironmentZone>
    interface IRequire<IRiderFeatureZone>
    interface IRequire<IRiderProductEnvironmentZone>
    interface IRequire<ISinceClr4HostZone>
