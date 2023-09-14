namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Platform.RdFramework
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Psi.CSharp
open JetBrains.ReSharper.Psi.RegExp

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<ICodeEditingZone>
    interface IRequire<ILanguageCSharpZone>
    interface IRequire<ILanguageRegExpZone>
    interface IRequire<IRdFrameworkZone>
    interface IRequire<ISinceClr4HostZone>
