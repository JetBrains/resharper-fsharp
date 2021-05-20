namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type InvertIfTest() =
    inherit FSharpContextActionExecuteTestBase<InvertIfAction>()

    override x.ExtraPath = "invertIf"

    [<Test>] member x.``Literal 01 - True``() = x.DoNamedTest()
    [<Test>] member x.``Literal 02 - False``() = x.DoNamedTest()

    [<Test>] member x.``Reference 01``() = x.DoNamedTest()
    [<Test>] member x.``Reference 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Reference 03 - Qualified``() = x.DoNamedTest()
    [<Test>] member x.``Reference 04 - Qualified with method``() = x.DoNamedTest()

    [<Test>] member x.``App - Not 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Not 02 - Pipe right``() = x.DoNamedTest()
    [<Test>] member x.``App - Not 03 - Pipe left``() = x.DoNamedTest()
    [<Test>] member x.``App - Not 04 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``App 01``() = x.DoNamedTest()
    [<Test>] member x.``App 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``App 03 - Inside other expr``() = x.DoNamedTest()

    [<Test>] member x.``Logic operators 01``() = x.DoNamedTest()

    [<Test>] member x.``Condition - If 01``() = x.DoNamedTest()

    [<Test>] member x.``Else - If 01``() = x.DoNamedTest()
    [<Test>] member x.``Else - If 02``() = x.DoNamedTest()

    [<Test>] member x.``Single line expressions 01``() = x.DoNamedTest()
    [<Test>] member x.``Single line expressions 02 - App``() = x.DoNamedTest()
    [<Test>] member x.``Single line expressions 03 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Single line expressions 04 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Single line expressions 05 - Lazy``() = x.DoNamedTest()

    [<Test>] member x.``Deindent - Single line expressions 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Deindent - Single line expressions 02 - Comment``() = x.DoNamedTest()
