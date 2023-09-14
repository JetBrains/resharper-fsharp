namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.Rider.Backend.Env

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<ILanguageFSharpZone>
    interface IRequire<IResharperHostCoreFeatureZone>
    interface IRequire<IRiderFeatureEnvironmentZone>
