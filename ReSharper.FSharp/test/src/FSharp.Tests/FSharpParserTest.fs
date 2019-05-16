namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Refactorings

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

    [<Test>] member x.``Expr - Const 01 - Unit``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Null``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Paren 01``() = x.DoNamedTest()
