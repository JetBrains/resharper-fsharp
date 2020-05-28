namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

type GenerateMissingInterfaceImplementationTest() =
    inherit FSharpContextActionExecuteTestBase<GenerateMissingInterfaceImplementation>()

    override x.ExtraPath = "generateMissingInterfaceImplementation"

    [<Test>] member x.``Empty interface implementation``() = x.DoNamedTest()
    // TODO: Test a case with generics
    
type GenerateMissingInterfaceImplementationAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<GenerateMissingInterfaceImplementation>()

    override x.ExtraPath = "generateMissingInterfaceImplementation"

    [<Test>] member x.``Partial interface implementation``() = x.DoNamedTest()
    [<Test>] member x.``Complete interface implementation``() = x.DoNamedTest()
    [<Test>] member x.``Members on class``() = x.DoNamedTest()
    // TODO: `interface SomeClassNotAnInterface with` shouldn't be available