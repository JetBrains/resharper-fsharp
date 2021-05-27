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

[<ZoneActivator>]
type PsiFeatureTestZoneActivator() =
    interface IActivate<PsiFeatureTestZone> with
        member x.ActivatorEnabled() = true


[<ZoneActivator>]
type FSharpZoneActivator() =
    interface IActivate<ILanguageFSharpZone> with
        member x.ActivatorEnabled() = true


[<ShellComponent>]
type FSharpFileServiceStub() =
    interface IHideImplementation<FSharpFileService>

[<SetUpFixture>]
type PsiFeaturesTestEnvironmentAssembly() =
    inherit ExtensionTestEnvironmentAssembly<IFSharpTestsZone>()
