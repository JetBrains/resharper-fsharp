namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Common

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Environment
open JetBrains.ReSharper.TestFramework
open JetBrains.TestFramework
open JetBrains.TestFramework.Application.Zones
open NUnit.Framework

[<assembly: NUnit.Framework.RequiresSTA>]
    do()

[<ZoneDefinition>]
type IFSharpTestsZone =
    inherit ITestsZone

[<ZoneActivator>]
type PsiFeatureTestZoneActivator() = 
    interface IActivate<PsiFeatureTestZone> with
        member x.ActivatorEnabled() = true

[<SetUpFixture>]
type PsiFeaturesTestEnvironmentAssembly() = 
    inherit ExtensionTestEnvironmentAssembly<IFSharpTestsZone>()
