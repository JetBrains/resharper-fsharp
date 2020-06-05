namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

type GenerateMissingInterfaceImplementationTest() =
    inherit FSharpContextActionExecuteTestBase<GenerateMissingInterfaceImplementation>()

    override x.ExtraPath = "generateMissingInterfaceImplementation"

    [<Test>] member x.``Empty interface implementation - concrete``() = x.DoNamedTest()
    [<Test>] member x.``Empty interface implementation - generics``() = x.DoNamedTest()
    
type GenerateMissingInterfaceImplementationAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<GenerateMissingInterfaceImplementation>()

    override x.ExtraPath = "generateMissingInterfaceImplementation"

    [<Test>] member x.``Partial interface implementation``() = x.DoNamedTest()
    [<Test>] member x.``Complete interface implementation``() = x.DoNamedTest()
    [<Test>] member x.``Members on class``() = x.DoNamedTest()
    [<Test>] member x.``Interface implementation with class name``() = x.DoNamedTest()