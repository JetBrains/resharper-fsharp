namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Plugins.FSharp

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IFSharpPluginZone>
    
namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Services

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ReSharper.Plugins.FSharp

[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IFSharpPluginZone>
