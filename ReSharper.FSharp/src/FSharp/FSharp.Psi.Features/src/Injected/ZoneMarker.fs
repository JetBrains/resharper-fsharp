namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Psi.RegExp

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<ILanguageRegExpZone>
