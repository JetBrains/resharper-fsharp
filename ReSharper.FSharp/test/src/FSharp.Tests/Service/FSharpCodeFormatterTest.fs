namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestSettingsKey(typeof<FSharpFormatSettingsKey>)>]
type FSharpCodeFormatterTest() =
    inherit CodeFormatterWithExplicitSettingsTestBase<FSharpLanguage>()

    override x.RelativeTestDataPath = "features/service/codeFormatter"

    override x.DoNamedTest() =
        use cookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
        base.DoNamedTest()

    [<Test>] member x.``Expr - App - CompExpr 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Nested 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match 02 - Last clause expr``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Unit 01``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - List 01 - Empty``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - List 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - List 03``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Pattern - List 04``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Tuple - IsInst 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Tuple - ListCons 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Tuple - Align 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Tuple 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Unit 01``() = x.DoNamedTest() // todo: move right paren further?

    [<Test>] member x.``Type - Array 01``() = x.DoNamedTest() // todo: nested array indent?
    [<Test>] member x.``Type - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Type - Named 01``() = x.DoNamedTest()
    [<Test>] member x.``Type - Tuple 01``() = x.DoNamedTest()

    [<Test>] member x.``Blank lines - Module - Nested 01``() = x.DoNamedTest()
    [<Test>] member x.``Blank lines - Module members 01 - Different kinds``() = x.DoNamedTest()
    [<Test>] member x.``Blank lines - Module members 02 - Type groups``() = x.DoNamedTest()
    [<Test>] member x.``Blank lines - Module members 03 - Binding groups``() = x.DoNamedTest()
    [<Test>] member x.``Blank lines - Namespace 01``() = x.DoNamedTest()

    [<Test>] member x.``Module - Nested - Members``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top 02``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 01``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 02 - Global``() = x.DoNamedTest()

    [<Test>] member x.``Module abbreviation 01``() = x.DoNamedTest()
    [<Test>] member x.``Open 01``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Class 01``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 02 - Access modifier``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 03 - Spaces``() = x.DoNamedTest() // todo: add spaces in decl start

    [<Test>] member x.``Type decl - Exception 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Exception 02 - Fields``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union 01 - Spaces``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union 02 - Indent``() = x.DoNamedTest()

    [<Test>] member x.``Top binding indent 01 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Top binding indent 02 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Top binding indent 03 - Big indent``() = x.DoNamedTest()
    [<Test>] member x.``Local binding indent 01 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Local binding indent 02 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Local binding indent 03 - Big indent``() = x.DoNamedTest()
    [<Test>] member x.``Let module decl binding indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Let expr binding indent 01 - Correct indent``() = x.DoNamedTest()

    [<Test>] member x.``For expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``For expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``For expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``ForEach expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``ForEach expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``ForEach expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``While expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``While expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``While expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Do expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Do expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Do expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Assert expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Assert expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Assert expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Lazy expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Lazy expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Lazy expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Set expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Set expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Set expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``TryWith expr indent 01``() = x.DoNamedTest()
    [<Test>] member x.``TryFinally expr indent 01 - Correct indent``() = x.DoNamedTest()

    [<Test>] member x.``IfThenElse expr indent 01``() = x.DoNamedTest()
    [<Test>] member x.``IfThenElse expr indent 02``() = x.DoNamedTest()
    [<Test>] member x.``IfThenElse expr indent 03 - Elif``() = x.DoNamedTest()

    [<Test>] member x.``MatchClause expr indent 02 - TryWith``() = x.DoNamedTest()
    [<Test>] member x.``MatchClause expr indent 03 - TryWith - Clause on the same line``() = x.DoNamedTest()
    [<Test>] member x.``MatchClause expr indent 06 - When``() = x.DoNamedTest()

    [<Test>] member x.``Lambda expr indent 01 - Without offset``() = x.DoNamedTest()
    [<Test>] member x.``Lambda expr indent 02 - With offset``() = x.DoNamedTest()

    [<Test>] member x.``Enum declaration indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Union declaration indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Union declaration indent 02 - Modifier``() = x.DoNamedTest()

    [<Test>] member x.``Sequential expr alignment 01 - No separators``() = x.DoNamedTest()
    [<Test>] member x.``Sequential expr alignment 02 - Separators``() = x.DoNamedTest()
    [<Test>] member x.``Binary expr alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Binary expr alignment 02 - Pipe operator``() = x.DoNamedTest()
    [<Test>] member x.``Record declaration alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Record declaration alignment 02 - Semicolons``() = x.DoNamedTest()
    [<Test>] member x.``Record declaration alignment 03 - Mutable``() = x.DoNamedTest()
    [<Test>] member x.``Record expr alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Record expr alignment 02 - Copy``() = x.DoNamedTest()
    [<Test>] member x.``Anon record expr alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Anon record expr alignment 02 - Copy``() = x.DoNamedTest()

    [<Test>] member x.``Type members 01``() = x.DoNamedTest()
    [<Test>] member x.``Type members 02 - Interface``() = x.DoNamedTest()
