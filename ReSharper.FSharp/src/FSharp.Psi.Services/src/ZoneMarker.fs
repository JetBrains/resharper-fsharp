namespace JetBrains.ReSharper.Plugins.FSharp.Psi

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.DocumentModel
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Rider.Backend.Env

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IDocumentModelZone>
    interface IRequire<ILanguageFSharpZone>
    interface IRequire<IResharperHostCoreFeatureZone>
    interface IRequire<IRiderFeatureEnvironmentZone>
    interface IRequire<PsiFeaturesImplZone>
