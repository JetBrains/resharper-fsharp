namespace JetBrains.ReSharper.Plugins.FSharp.Settings

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.Rider.Backend.Env
open JetBrains.Rider.Model

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<ILanguageFSharpZone>
    interface IRequire<IResharperHostCoreFeatureZone>
    interface IRequire<IRiderFeatureEnvironmentZone>
    interface IRequire<IRiderModelZone>
