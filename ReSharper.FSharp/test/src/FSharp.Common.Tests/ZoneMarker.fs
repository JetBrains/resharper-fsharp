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
type IFSharpTestsZone =
    inherit ITestsEnvZone

[<ZoneActivator(Lifecycle.DemandReclaimable, Creation.AnyThread, Access.AnyThread)>]
type PsiFeatureTestZoneActivator() =
    interface IActivate<PsiFeatureTestZone>

[<ZoneActivator(Lifecycle.DemandReclaimable, Creation.AnyThread, Access.AnyThread)>]
type FSharpZoneActivator() =
    interface IActivate<ILanguageFSharpZone>

[<ShellComponent>]
type FSharpFileServiceStub() =
    interface IHideImplementation<FSharpFileService>

[<SetUpFixture>]
type PsiFeaturesTestEnvironmentAssembly() =
    inherit ExtensionTestEnvironmentAssembly<IFSharpTestsZone>()
