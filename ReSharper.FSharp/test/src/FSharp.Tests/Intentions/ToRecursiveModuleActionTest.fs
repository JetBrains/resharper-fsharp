namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type ToRecursiveModuleActionExecuteTest() =
    inherit FSharpContextActionExecuteTestBase<ToRecursiveModuleAction>()

    override x.ExtraPath = "toRecursiveModule"

    [<Test>] member x.``Module - Named 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested 01``() = x.DoNamedTest()
    [<Test>] member x.``Namespace - Global 01``() = x.DoNamedTest()
    [<Test>] member x.``Namespace - Named 01``() = x.DoNamedTest()


type ToRecursiveModuleActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ToRecursiveModuleAction>()

    override x.ExtraPath = "toRecursiveModule"

    [<Test>] member x.``Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 01``() = x.DoNamedTest()
