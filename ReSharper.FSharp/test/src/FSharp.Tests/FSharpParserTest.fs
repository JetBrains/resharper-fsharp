namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpParserTest() =
    inherit ParserTestBase<FSharpLanguage>()

    override x.RelativeTestDataPath = "parsing"

    [<Test>] member x.``Module - Anon 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top level 01``() = x.DoNamedTest()

    [<Test>] member x.``Let 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Let 02 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Let 03 - Typed, var type``() = x.DoNamedTest()
    [<Test>] member x.``Let 04 - Typed, var type, space``() = x.DoNamedTest()
    [<Test>] member x.``Let 05 - Typed, space``() = x.DoNamedTest()

    [<Test>] member x.``Let - Local 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local 02 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local 03 - Typed expr``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Paren 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Quote 01 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Quote 02 - Untyped``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Const 01 - Unit``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Typed 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - While 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Do 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Assert 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - TryWith 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - TryFinally 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lazy 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - IfThenElse 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Ident 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Null``() = x.DoNamedTest()
    [<Test>] member x.``Expr - AddressOf 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Fixed 01 - Simple``() = x.DoNamedTest()
