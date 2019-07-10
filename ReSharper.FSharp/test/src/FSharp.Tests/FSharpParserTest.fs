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

    [<Test>] member x.``Let - Rec 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Rec 02 - And``() = x.DoNamedTest()

    [<Test>] member x.``Let - Local 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local 02 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local 03 - Typed expr``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Paren 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Quote 01 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Quote 02 - Untyped``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Tuple 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Const 01 - Unit``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Typed 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - While 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - CompExpr 01 - Return``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Lambda 01 - Single id``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 02 - Single wild``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 03 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 04 - Long id pattern``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 05 - Multiple wilds``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 06 - Wild and named pats 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 07 - Wild and named pats 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 08 - Match expr``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 09 - Long id with or pat``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 10 - Multiple tuples``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 11 - Two wilds``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 12 - Paren``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 13 - Two parens``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 14 - Nested parens``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 15 - Unit``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 16 - Unit in parens``() = x.DoNamedTest()

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

    [<Test>] member x.``Expr - DotIndexerGet 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - DotIndexerGet 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - DotIndexerGet 03``() = x.DoNamedTest()

    [<Test>] member x.``Expr - DotIndexerSet 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - DotIndexerSet 02 - Record``() = x.DoNamedTest()

    [<Test>] member x.``Binding - Return type 01``() = x.DoNamedTest()
    [<Test>] member x.``Binding - Return type 02 - Attrs``() = x.DoNamedTest()
    [<Test>] member x.``Binding - Return type 03 - Attrs, wild type``() = x.DoNamedTest()

    [<Test>] member x.``Attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 02 - Simple arg``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 03 - Simple arg - No parens``() = x.DoNamedTest()
