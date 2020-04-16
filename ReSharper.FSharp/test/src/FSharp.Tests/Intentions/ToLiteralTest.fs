namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

type ToLiteralTest() =
    inherit FSharpContextActionExecuteTestBase<ToLiteralAction>()

    override x.ExtraPath = "toLiteral"

    [<Test>] member x.``Let - Attributes 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Attributes 02``() = x.DoNamedTest()
    [<Test>] member x.``Let - Attributes 03``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Let - Attributes 04``() = x.DoNamedTest()
    [<Test>] member x.``Let - Attributes 05``() = x.DoNamedTest()
    [<Test>] member x.``Let - Attributes 06``() = x.DoNamedTest()

    [<Test>] member x.``Let 01``() = x.DoNamedTest()

type ToLiteralTestAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ToLiteralAction>()

    override x.ExtraPath = "toLiteral"

    [<Test>] member x.``Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Attributes 01``() = x.DoNamedTest()
