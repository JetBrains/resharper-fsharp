namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions

open System.Threading
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.TestFramework
open NUnit.Framework

[<assembly: Apartment(ApartmentState.STA)>]
do()

[<SetUpFixture>]
type PsiFeaturesTestEnvironmentAssembly() =
    inherit ExtensionTestEnvironmentAssembly<IFSharpTestsZone>()

module ForceAssemblyReference =
    let _ = FSharpErrorsStage.redundantParensEnabledKey
