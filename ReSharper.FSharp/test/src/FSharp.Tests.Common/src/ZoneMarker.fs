namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Environment
open JetBrains.ReSharper.Daemon.Syntax
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.TestFramework
open JetBrains.TestFramework.Application.Zones

[<ZoneDefinition>]
type IFSharpTestsZone =
    inherit ITestsEnvZone

[<ZoneActivator>]
type PsiFeatureTestZoneActivator() =
    interface IActivate<PsiFeatureTestZone>

[<ZoneActivator>]
type FSharpZoneActivator() =
    interface IActivate<ILanguageFSharpZone>

[<ZoneActivator>]
type SyntaxHighlightingZoneActivator() =
    interface IActivate<ISyntaxHighlightingZone>
