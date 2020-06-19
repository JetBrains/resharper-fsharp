namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

type LetToUseTest() =
    inherit FSharpContextActionExecuteTestBase<LetToUseAction>()

    override x.ExtraPath = "letToUse"

    [<Test>] member x.``Ref 01``() = x.DoNamedTest()
    [<Test>] member x.``As 01``() = x.DoNamedTest()

type LetToUseAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<LetToUseAction>()

    override x.ExtraPath = "letToUse"

    [<Test>] member x.``Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Bindings 01``() = x.DoNamedTest()
