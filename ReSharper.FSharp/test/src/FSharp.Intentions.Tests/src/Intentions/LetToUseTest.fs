namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type LetToUseTest() =
    inherit FSharpContextActionExecuteTestBase<LetToUseAction>()

    override x.ExtraPath = "letToUse"

    [<Test>] member x.``Ref 01``() = x.DoNamedTest()
    [<Test>] member x.``Ref 02``() = x.DoNamedTest()
    [<Test>] member x.``Ref 03``() = x.DoNamedTest()
    [<Test>] member x.``Ref 04``() = x.DoNamedTest()

type LetToUseAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<LetToUseAction>()

    override x.ExtraPath = "letToUse"

    [<Test>] member x.``Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Bindings 01``() = x.DoNamedTest()
