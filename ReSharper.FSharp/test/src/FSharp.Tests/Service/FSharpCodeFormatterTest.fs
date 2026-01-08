namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.CodeCleanup
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

    [<Test>] member x.``Expr - App - Binary 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - CompExpr 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - CompExpr 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Nested 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Nested 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 04``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 05``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 06``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Let 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Let 03``() = x.DoNamedTest()
    [<Test; TestSettings("{FormatProfile:INDENT}")>] member x.``Expr - List 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match 02 - Last clause expr``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Obj 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Obj 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Obj 03 - Interface``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Obj 04 - Interface``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Op - Deref 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Ref 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Ref 02 - Invocation``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Ref 04``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Try With 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Unit 01``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - List 01 - Empty``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - List 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - List 03``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Pattern - List 04``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Param owner 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Tuple - IsInst 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Tuple - ListCons 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Tuple - Align 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Tuple 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Unit 01``() = x.DoNamedTest() // todo: move right paren further?

    [<Test>] member x.``Type - Array 01``() = x.DoNamedTest()
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
    [<Test>] member x.``Type decl - Class 02``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Class 03``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 02 - Access modifier``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 03 - Spaces``() = x.DoNamedTest() // todo: add spaces in decl start
    [<Test>] member x.``Type decl - Enum 04``() = x.DoNamedTest() // todo: add spaces in decl start

    [<Test>] member x.``Type decl - Exception 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Exception 02 - Fields``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Record 01``() = x.DoNamedTest() // todo: fix KeepExistingLineBreakBeforeDeclarationBody
    [<Test>] member x.``Type decl - Record 02``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Record 03``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Record 04``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Record 05``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Record 06``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Repr 01 - Comment``() = x.DoNamedTest()

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
    [<Test>] member x.``Record expr alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Record expr alignment 02 - Copy``() = x.DoNamedTest()
    [<Test>] member x.``Anon record expr alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Anon record expr alignment 02 - Copy``() = x.DoNamedTest()

    [<Test>] member x.``Type members 01``() = x.DoNamedTest()
    [<Test>] member x.``Type members 02 - Interface``() = x.DoNamedTest()
    [<Test>] member x.``Type members 03``() = x.DoNamedTest()

    [<Test>] member x.``File 01``() = x.DoNamedTest()
    [<Test>] member x.``File 02``() = x.DoNamedTest()
    [<Test>] member x.``File 03``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``File 05 - Double indent``() = x.DoNamedTest() // todo: formatter: fix double indent
    [<Test>] member x.``File 05``() = x.DoNamedTest()
    [<Test>] member x.``File 06``() = x.DoNamedTest()
    [<Test>] member x.``File 07``() = x.DoNamedTest()
    [<Test>] member x.``Let binding 01 - XmlDoc``() = x.DoNamedTest()
    [<Test>] member x.``Let binding 02 - Group``() = x.DoNamedTest()
    [<Test>] member x.``Let binding 03 - Type params``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Or 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Group 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Fields 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Paren 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Paren 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Paren 03``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested - Members 02``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top 03``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top 04``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top - Attr 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top - Attr 02``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top - Attr 03``() = x.DoNamedTest()

[<FSharpTest; TestSettingsKey(typeof<FSharpFormatSettingsKey>)>]
type FSharpCodeCleanupTest() =
    inherit CodeCleanupTestBase()

    override x.RelativeTestDataPath = "features/service/codeCleanup"

    [<Test; Explicit>] member x.``Record 01``() = x.DoNamedTest()
