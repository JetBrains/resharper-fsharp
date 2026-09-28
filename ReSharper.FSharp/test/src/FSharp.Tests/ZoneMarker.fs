namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System.Threading
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.TestFramework
open JetBrains.TestFramework.Build.Nunit
open NUnit.Framework

[<assembly: Apartment(ApartmentState.STA)>]
[<assembly: NUnitRunTestsOnNetFrameworkOrMonoRuntime>]
do()

[<SetUpFixture>]
type PsiFeaturesTestEnvironmentAssembly() =
    inherit ExtensionTestEnvironmentAssembly<IFSharpTestsEnvZone>()
