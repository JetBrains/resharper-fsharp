namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

type ToLiteralTest() =
    inherit FSharpContextActionExecuteTestBase<ToLiteralAction>()

    override x.ExtraPath = "toLiteral"

    [<Test>] member x.``Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Let 02 - Existing attributes``() = x.DoNamedTest()
    [<Test>] member x.``Let 03 - Existing attributes``() = x.DoNamedTest()
    [<Test>] member x.``Let 04 - Mutable``() = x.DoNamedTest()
    [<Test>] member x.``Let 05 - Private``() = x.DoNamedTest()
    [<Test>] member x.``Let 06 - Mutable private``() = x.DoNamedTest()

type ToLiteralAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ToLiteralAction>()

    override x.ExtraPath = "toLiteral"

    [<Test>] member x.``Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Attributes 01``() = x.DoNamedTest()
