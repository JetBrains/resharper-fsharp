namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System.Threading
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.TestFramework
open NUnit.Framework

[<assembly: Apartment(ApartmentState.STA)>]
do()

[<SetUpFixture>]
type PsiFeaturesTestEnvironmentAssembly() =
    inherit ExtensionTestEnvironmentAssembly<IFSharpTestsEnvZone>()
