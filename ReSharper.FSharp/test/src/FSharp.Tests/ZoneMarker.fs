namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.Application
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Environment
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.TestFramework
open JetBrains.TestFramework
open JetBrains.TestFramework.Application.Zones
open NUnit.Framework

[<ZoneDefinition>]
type IFSharpTestsZone =
    inherit ITestsEnvZone

[<ZoneActivator>]
type PsiFeatureTestZoneActivator() =
    interface IActivate<PsiFeatureTestZone>

[<ZoneActivator>]
type FSharpZoneActivator() =
    interface IActivate<ILanguageFSharpZone>

[<SetUpFixture>]
type PsiFeaturesTestEnvironmentAssembly() =
    inherit ExtensionTestEnvironmentAssembly<IFSharpTestsZone>()
