namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions

open System.Threading
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.TestFramework
open NUnit.Framework
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

[<assembly: Apartment(ApartmentState.STA)>]
do()

[<SetUpFixture>]
type PsiFeaturesTestEnvironmentAssembly() =
    inherit ExtensionTestEnvironmentAssembly<IFSharpTestsEnvZone>()

module ForceAssemblyReference =
    let _ = FSharpErrorsStage.redundantParensEnabledKey
   
// Explicit reference is required to load Psi.Features assembly and construct components from that assembly
module ExplicitReferenceToPsiFeatures =
    let _ = typeof<FSharpLanguageService>
