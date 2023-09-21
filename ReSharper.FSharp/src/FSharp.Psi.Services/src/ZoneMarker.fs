namespace JetBrains.ReSharper.Plugins.FSharp.Psi

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Resources.Shell

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IDocumentModelZone>
    interface IRequire<ILanguageFSharpZone>
    interface IRequire<PsiFeaturesImplZone>
