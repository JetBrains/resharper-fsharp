namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Psi.CSharp

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<ILanguageFSharpZone>
    interface IRequire<ILanguageCSharpZone>
    interface IRequire<ICodeEditingZone>
