namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Common

open System.Threading
open JetBrains.Application
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Components
open JetBrains.Application.Environment
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.TestFramework
open JetBrains.TestFramework
open JetBrains.TestFramework.Application.Zones
open NUnit.Framework

[<assembly: Apartment(ApartmentState.STA)>]
do()

[<ZoneDefinition>]
type ICommonFSharpTestsEnvZone =
    inherit ITestsEnvZone

[<ZoneDefinition>]
type ICommonTestFSharpPluginZone =
    inherit IZone
    inherit IRequire<IFSharpPluginZone>
    inherit IRequire<PsiFeatureTestZone>
    
[<ZoneActivator>]
[<ZoneMarker(typeof<ICommonFSharpTestsEnvZone>)>]
type FSharpTestZoneActivator() =
    interface IActivate<ICommonTestFSharpPluginZone>

[<SetUpFixture>]
type PsiFeaturesTestEnvironmentAssembly() =
    inherit ExtensionTestEnvironmentAssembly<ICommonFSharpTestsEnvZone>()
