namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Environment
open JetBrains.ReSharper.Daemon.Syntax
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.TestFramework
open JetBrains.TestFramework.Application.Zones

[<ZoneDefinition>]
type IFSharpTestsEnvZone =
    inherit ITestsEnvZone

[<ZoneDefinition>]
type ITestFSharpPluginZone =
    inherit IZone
    inherit IRequire<IFSharpPluginZone>
    inherit IRequire<PsiFeatureTestZone>
    
[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<ITestFSharpPluginZone>
