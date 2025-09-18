namespace JetBrains.ReSharper.Plugins.FSharp

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Environment
open JetBrains.ReSharper.Plugins.FSharp.Tests

[<ZoneActivator>]
[<ZoneMarker(typeof<IFSharpTestsEnvZone>)>]
type FSharpTestZoneActivator() =
    interface IActivate<ITestFSharpPluginZone>
