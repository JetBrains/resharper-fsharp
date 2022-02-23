namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpParserTest() =
    inherit ParserTestBase<FSharpLanguage>()

    override x.RelativeTestDataPath = "parsing"

    [<Test>] member x.``Module - Anon 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Anon 02``() = x.DoNamedTest()

    [<Test>] member x.``Module - Multiple 01 - XmlDoc``() = x.DoNamedTest()

    [<Test>] member x.``Module - Nested - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested - XmlDoc 02``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested - XmlDoc 03``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested 01``() = x.DoNamedTest()

    [<Test>] member x.``Module - Top level - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top level - XmlDoc 02 - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top level - XmlDoc 03 - Separated by attributes``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top level - XmlDoc 04 - Attributes before``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top level - XmlDoc 05 - Attributes before``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top level 01``() = x.DoNamedTest()

    [<Test>] member x.``Namespace 01``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 02 - Qualifier``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 03 - Multiple``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 04 - Multiple qualifiers``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 05 - Global``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 06 - Global, type``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 07 - Global, Multiple``() = x.DoNamedTest()

    [<Test>] member x.``Type decl group 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl group 02 - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Type decl group 03 - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl group 03 - XmlDoc 02``() = x.DoNamedTest()
    [<Test>] member x.``Type decl group 03 - XmlDoc 03 - With Attributes``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Exception - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Exception 01 - Empty``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Exception 02 - Fields``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Exception 03 - Members``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Extension - Type parameters 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Extension - Type parameters 02``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Extension - Type parameters 03``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Extension - Type parameters 04``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Extension - Type parameters 05``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Extension 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Extension 02 - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Extension 03 - Member attributes``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Delegate - Ctor 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Delegate - Ctor 02 - Parameter``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Delegate 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Delegate 02``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - XmlDoc 02 - Multiple lines``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - XmlDoc 03 - Attribute``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - XmlDoc 04 - Separated by attributes``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Il 01``() = x.DoNamedTest()

    [<Test>] member x.``Type Parameters - Constraints - Outside list 01``() = x.DoNamedTest()
    [<Test>] member x.``Type Parameters - Constraints - Outside list 02 - Ctor``() = x.DoNamedTest()
    [<Test>] member x.``Type Parameters - Constraints - Outside list 03 - Internal Ctor``() = x.DoNamedTest()
    [<Test>] member x.``Type Parameters - Constraints - Outside list 04 - Ctor with XmlDoc``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Constraints 01 - Struct``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Constraints 02 - Member``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Constraints 03 - Or``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Constraints 04 - Or and member``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Constraints 05 - Ctor``() = x.DoNamedTest()
    [<Test>] member x.``Type Parameters 01``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Attributes 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Attributes 02``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Let binding - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Let binding - Value 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Let binding - Value 02 - Upper``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Let bindings 01``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Empty 01``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Enum - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 01 - With first bar``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 02 - Without first bar``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 03 - Case attributes``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 04 - Private repr``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Interface 01``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Union - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union - XmlDoc 02 - Multiple cases``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union - XmlDoc 03 - Private repr``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union - XmlDoc 04 - No bar``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union - XmlDoc 05 - Fields``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union 02 - Modifier``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union 03 - No first bar``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union 04 - No first bar with modifier``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union 05 - Case attributes``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union 06 - Fields``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Record - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Record - XmlDoc 02 - Attribute``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Record - XmlDoc 03 - Wrong range``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Record 01 - Single line``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Record 02 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Record 03 - Multiline, semicolons``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Record 04 - Attribute``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Struct 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Struct 02 - Fields``() = x.DoNamedTest()

    [<Test>] member x.``Let 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Let 02 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Let 03 - Typed, var type``() = x.DoNamedTest()
    [<Test>] member x.``Let 04 - Typed, var type, space``() = x.DoNamedTest()
    [<Test>] member x.``Let 05 - Typed, space``() = x.DoNamedTest()
    [<Test>] member x.``Let 06 - Unit param``() = x.DoNamedTest()

    [<Test>] member x.``Let - Rec 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Rec 02 - And``() = x.DoNamedTest()

    [<Test>] member x.``Let - As 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - As 02``() = x.DoNamedTest()

    [<Test>] member x.``Let - Local - Parameters 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local - Parameters 02``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local - Upper case 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local - Upper case 02 - Parameters``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local - XmlDoc 02 - In method``() = x.DoNamedTest()

    [<Test>] member x.``Let - Local 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local 02 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local 03 - Typed expr``() = x.DoNamedTest()

    [<Test>] member x.``Let - Top 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Top 02 - Upper case``() = x.DoNamedTest()
    [<Test>] member x.``Let - Top 03 - XmlDoc 01``() = x.DoNamedTest()

    [<Test>] member x.``Let - Type parameters - Local - Parameters 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type parameters - Local - Parameters 02 - Upper case``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type parameters - Local - Ref 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type parameters - Local - Ref 02 - Upper case``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type parameters - Top - Parameters 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type parameters - Top - Parameters 02 - Upper case``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type parameters - Top - Ref 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type parameters - Top - Ref 02 - Upper case``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Active pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Active pattern 02 - Multiple cases``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Active pattern 03 - Wild``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Active pattern 04 - Qualified``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Paren 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Quote 01 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Quote 02 - Untyped``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Tuple 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Tuple 03 - Struct``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Const - Numbers 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Const - String 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Const - Unit 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Const - Unit 02 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Typed 01 - Simple``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Anon record 01 - Single line``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Anon record 02 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Anon record 03 - With copy info``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Record - Inherit 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record 01 - Single Line``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record 02 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record 03 - Multiline, semicolons``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record 04 - Empty``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record 05 - Single Line with end semicolon``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record 06 - With qualifier``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record 07 - With copy info``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record 08 - Spaces before semicolon``() = x.DoNamedTest()

    [<Test>] member x.``Expr - While 01 - Simple``() = x.DoNamedTest()

    [<Test>] member x.``Expr - For 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - ForEach - Range 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - ForEach - Range 02 - Step``() = x.DoNamedTest()
    [<Test>] member x.``Expr - ForEach 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - CompExpr - Arg 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - CompExpr - Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - CompExpr - Range 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - CompExpr - Range 02 - Step``() = x.DoNamedTest()
    [<Test>] member x.``Expr - CompExpr 01 - Return``() = x.DoNamedTest()

    [<Test>] member x.``Expr - CompExpr - AndBang 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - CompExpr - For 01``() = x.DoNamedTest()

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
    [<Test>] member x.``Expr - Lambda 17 - As``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 18 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 19 - Typed - Multiple params``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 20 - Two units``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 21 - Nested types``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 22 - Attribute``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 23 - Attribute, Typed``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 24 - Unit in nested parens``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 25 - Multiple matches``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 26 - Multiple matches``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 27 - Multiple matches, nested``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 28 - Typed tuple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 29 - Uppercase``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 30 - Qualified``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Match 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match 02 - Simple pat``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match 03 - When Expr``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match 04 - Multiple When clauses``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Match lambda 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match lambda 02 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match lambda 03 - Multiple clauses``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Do 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Do 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Do 03``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Assert 01 - Simple``() = x.DoNamedTest()

    [<Test>] member x.``Expr - LetOrUse - Attributes 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - LetOrUse 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Trait call 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Trait call 02 - Nested parens``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Trait call 03 - Method``() = x.DoNamedTest()

    [<Test>] member x.``Expr - TryWith 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - TryWith 02 - Parameters``() = x.DoNamedTest()

    [<Test>] member x.``Expr - TryFinally 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lazy 01 - Simple``() = x.DoNamedTest()

    [<Test>] member x.``Expr - If 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - IfThenElse - Elif 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - IfThenElse - Elif 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - IfThenElse 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - IfThenElse 02 - Nested 01 - Single line``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Ident 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - LongIdent 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - LongIdentSet 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Cast - Downcast 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Cast - Upcast 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Null``() = x.DoNamedTest()
    [<Test>] member x.``Expr - AddressOf 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Fixed 01 - Simple``() = x.DoNamedTest()

    [<Test>] member x.``Expr - DotGet 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - DotSet 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - App - Logic 01 - And``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Logic 02 - Or``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Nested 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - App - Precedence 01 - High``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Precedence 02 - Low``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Precedence 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Precedence 04 - High - List``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Precedence 05 - High - Multiple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Precedence 06 - High - Multiple``() = x.DoNamedTest()

    [<Test>] member x.``Expr - App - Prefix op - Binary 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Prefix op - Binary 02 - Spaces``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Prefix op 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - App - Indexer 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Indexer 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Indexer 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - Indexer 04``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - List 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - App - List 02``() = x.DoNamedTest()

    [<Test>] member x.``Expr - TypeApp 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - TypeApp 02 - Lid``() = x.DoNamedTest()
    [<Test>] member x.``Expr - TypeApp 03 - DotGet``() = x.DoNamedTest()

    [<Test>] member x.``Expr - DotIndexerGet 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - DotIndexerGet 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - DotIndexerGet 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - DotIndexerGet 04 - Multiple args``() = x.DoNamedTest()
    [<Test>] member x.``Expr - DotIndexerGet 05 - Typed``() = x.DoNamedTest()

    [<Test>] member x.``Expr - DotIndexerSet 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - DotIndexerSet 02 - Record``() = x.DoNamedTest()
    [<Test>] member x.``Expr - DotIndexerSet 03 - Two args``() = x.DoNamedTest()
    [<Test>] member x.``Expr - DotIndexerSet 04 - Three args``() = x.DoNamedTest()
    [<Test>] member x.``Expr - DotIndexerSet 05 - Tuple arg``() = x.DoNamedTest()

    [<Test>] member x.``Expr - DotNamedIndexerSet 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - NamedIndexerSet 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Sequential 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Sequential 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Sequential 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Sequential 04 - Let``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Sequential 05 - Let``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Sequential 06 - Let``() = x.DoNamedTest()

    [<Test>] member x.``Expr - List - Empty 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - List - Comprehension 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - List - Range sequence 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - List - Seq 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - List - Seq 02 - Implicit semi``() = x.DoNamedTest()
    [<Test>] member x.``Expr - List - Seq 03 - Implicit yield``() = x.DoNamedTest()
    [<Test>] member x.``Expr - List - Seq 04 - Yield``() = x.DoNamedTest()
    [<Test>] member x.``Expr - List - Seq 05 - ForEach``() = x.DoNamedTest()

    [<Test>] member x.``Expr - New 01 - Lid``() = x.DoNamedTest()
    [<Test>] member x.``Expr - New 02 - Generics``() = x.DoNamedTest()
    [<Test>] member x.``Expr - New 03 - Type parameter``() = x.DoNamedTest()

    [<Test>] member x.``Expr - ObjExpr 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - ObjExpr 02 - Interface``() = x.DoNamedTest()
    [<Test>] member x.``Expr - ObjExpr - Member - Property - Accessors 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - ObjExpr - Member - Property - Accessors 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - ObjExpr - Member - Property - Accessors 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - ObjExpr - Member - Property - Accessors 04 - Interface``() = x.DoNamedTest()
    [<Test>] member x.``Expr - ObjExpr - Member - Property - Accessors 05 - Interface``() = x.DoNamedTest()

    [<Test>] member x.``Expr - ImplicitZero 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - LetOrUseBang 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - LetOrUseBang 02 - Group``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Typeof 01 - Type parameter``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Slice - Beginning 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Slice - Beginning 02 - Negative``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Slice - End 01``() = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.Preview)>]
    [<Test>] member x.``Expr - Slice - End 02 - Negative``() = x.DoNamedTest()
    [<FSharpLanguageLevel(FSharpLanguageLevel.Preview)>]
    [<Test>] member x.``Expr - Slice - Negative 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Slice - Star 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Slice - Star 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Slice - Star 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Slice - Star 04``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Interpolated string 01 - Empty``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Interpolated string 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Interpolated string 03 - Whitespace``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Interpolated string 04 - Braces``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Interpolated string 05 - Multiple``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Interpolated string 06 - Multiple``() = x.DoNamedTest()

    [<Test>] member x.``Binding - Return type 01``() = x.DoNamedTest()
    [<Test>] member x.``Binding - Return type 02 - Attrs``() = x.DoNamedTest()
    [<Test>] member x.``Binding - Return type 03 - Attrs, wild type``() = x.DoNamedTest()

    [<Test>] member x.``Attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 02 - Simple arg``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 03 - Simple arg - No parens``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 04 - Qualifiers``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 05 - Qualifiers and arg``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 06 - Unit arg``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 07 - Semi``() = x.DoNamedTest()

    [<Test>] member x.``Attributes - Expression 01``() = x.DoNamedTest()
    [<Test>] member x.``Attributes - Expression 02 - Do``() = x.DoNamedTest()
    [<Test>] member x.``Attributes - Let bindings 01``() = x.DoNamedTest()
    [<Test>] member x.``Attributes - Let bindings 02``() = x.DoNamedTest()
    [<Test>] member x.``Attributes - Let bindings 03 - Modifiers``() = x.DoNamedTest()
    [<Test>] member x.``Attributes - Let bindings 04 - Multiple bindings``() = x.DoNamedTest()
    [<Test>] member x.``Attributes - Type let bindings 01``() = x.DoNamedTest()
    [<Test>] member x.``Attributes - Type let bindings 02``() = x.DoNamedTest()
    [<Test>] member x.``Attributes - Type let bindings 03 - Modifiers``() = x.DoNamedTest()
    [<Test>] member x.``Attributes - Type let bindings 04 - Multiple bindings``() = x.DoNamedTest()

    [<Test>] member x.``Module abbreviation 01``() = x.DoNamedTest()
    [<Test>] member x.``Module abbreviation 02``() = x.DoNamedTest()

    [<Test>] member x.``Types - XmlDoc 01``() = x.DoNamedTest()

    [<Test>] member x.``Types - Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Simple 02 - Long ident``() = x.DoNamedTest()
    [<Test>] member x.``Types - Simple 03 - Type app``() = x.DoNamedTest()
    [<Test>] member x.``Types - Simple 04 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Types - Simple 05 - Nested parens``() = x.DoNamedTest()

    [<Test>] member x.``Types - Paren - App 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Paren - App 02``() = x.DoNamedTest()
    [<Test>] member x.``Types - Paren - App 03``() = x.DoNamedTest()

    [<Test>] member x.``Types - Type app 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Type app 02 - ML-style``() = x.DoNamedTest()
    [<Test>] member x.``Types - Type app 03 - tuple``() = x.DoNamedTest()
    [<Test>] member x.``Types - Type app 04 - ML-style tuple``() = x.DoNamedTest()
    [<Test>] member x.``Types - Type app 05 - Qualifier and generics``() = x.DoNamedTest()
    [<Test>] member x.``Types - Type app 06 - Multiple qualifiers and generics``() = x.DoNamedTest()
    [<Test>] member x.``Types - Type app 07 - Var``() = x.DoNamedTest()

    [<Test>] member x.``Types - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Tuple 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Types - Tuple 03 - Nested 2``() = x.DoNamedTest()
    [<Test>] member x.``Types - Tuple 04 - Struct``() = x.DoNamedTest()
    [<Test>] member x.``Types - Tuple 05 - More items``() = x.DoNamedTest()
    [<Test>] member x.``Types - Tuple 06 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Types - Fun 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Fun 02``() = x.DoNamedTest()

    [<Test>] member x.``Types - Anon record 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Array 01``() = x.DoNamedTest()

    [<Test>] member x.``Types - Measure 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Measure 02 - Negate``() = x.DoNamedTest()

    [<Test>] member x.``Types - Constraints - Null 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Constraints - Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Constraints - Type 02 - No when``() = x.DoNamedTest()
    [<Test>] member x.``Types - Constraints - Reference 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Constraints - Struct 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Constraints - Comparison 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Constraints - Equality 01``() = x.DoNamedTest()

    [<Test>] member x.``Types - Static const - Const 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Static const - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Static const - Named 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Static const - Named 02``() = x.DoNamedTest()
    [<Test>] member x.``Types - Static const - Named 03``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Ctor - Primary - Parameters 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Primary - Parameters 02 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Primary - Parameters 03``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Primary - Parameters 04 - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Primary - Parameters 05 - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Primary - Parameters 06``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Primary - Self id 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Primary - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Primary - XmlDoc 02``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Primary 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Primary 02 - Modifier``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Primary 03 - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Primary 04 - Space``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Secondary - Parameters 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Secondary - Parameters 02``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Secondary 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Secondary 02 - Modifier``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Secondary 03 - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Secondary 04 - Self id``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Secondary 05``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Ctor - Secondary 06``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Inherit - Type 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Inherit - Type 02 - Qualified``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Inherit - Type 03 - Arguments``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Inherit - Type 04 - Type parameters 01``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Interface 02 - Members``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Do 01``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Let bindings - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Let bindings - XmlDoc 02 - Attrs``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Let bindings - XmlDoc 03 - Attrs``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Let bindings - XmlDoc 04``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Let bindings - Static 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Let bindings - Static 02 - Rec``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Let bindings 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Let bindings 02 - Rec``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Abstract 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Abstract 02 - Function``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Abstract 03 - Function return``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Member 01 - Wild self id``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Member - Method - Parameters - Optional 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Method - Parameters - Optional 02``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Method - Parameters - Optional 03 - Private``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Method - Parameters 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Method - Parameters 02``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Member - Method - Curried Parameters 01``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Member - Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Method 02 - Static``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Method 03 - Wild param``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Method 04 - Curried``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Member - Operator 01 - Add``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Operator 02 - Multiply``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Operator 03 - Subtract``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Operator 04 - Divide``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Member - Type parameters 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Type parameters 02``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - XmlDoc 01``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Auto Property - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Auto Property - XmlDoc 02 - Attribute``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Auto Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Auto Property 02``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Auto Property 03``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Auto Property 04``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Auto Property 05 - Access modifier``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Member - Property - Accessors 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Property - Accessors 02``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Property - Accessors 03``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Property - Accessors 04 - Access modifiers 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Property - Accessors 05 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Property - Accessors 06``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Property 02 - Static``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Property 03 - Access modifier``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Property 04 - Attributes``() = x.DoNamedTest()

    [<Test>] member x.``Module member - Do 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Do 02 - Let``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Do 03 - Let in do``() = x.DoNamedTest()

    [<Test>] member x.``Module member - Extern - Params 01 - Type``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern - Params 02 - Type and name``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern - Params 03 - Attribute``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern - Params 04 - Pointer``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern - Params 05 - Byref``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern - Params 06 - Void ptr``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern - Params 07 - Multiple``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern - Return 01 - Void``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern - Return 02 - Attribute``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern - Return 03 - Pointer``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern - XmlDoc 01 - With attributes``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern - XmlDoc 02 - Without attributes``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern 01``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Extern 02 - Access modifier``() = x.DoNamedTest()

    [<Test>] member x.``Module member - Let - XmlDoc 01``() = x.DoNamedTest()
    [<Test; Explicit("Remove extra space")>] member x.``Module member - Let - XmlDoc 02 - Separated by expression``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Let - Inline 01``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Let - Type parameters 01``() = x.DoNamedTest()

    [<Test>] member x.``Module member - Open 01``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Open 02 - Qualifier``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Open - Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Open - Type 02 - Type param``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Array 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Array 02 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Array 03 - Empty``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Array 04 - Single element``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - List 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - List 02 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - List 03 - Empty``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - List 04 - Single element``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Record 02 - Qualified name``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - As - Named 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - As - Parameter owner 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - As - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - As - Tuple 02``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Named args 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Named args 02 - Multiple``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Reference - Active pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Reference - Active pattern 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Reference - Active pattern 03 - Qualified``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Reference 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Reference 02 - Upper``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Reference 03 - Qualified``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Parameters owner 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Parameters owner 02 - Qualified``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Parameters owner 03 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Parameters owner 04 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Parameters owner 05 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - IsInst 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - IsInst 02 - Function``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - ListCons - Compiled name 01 - Let``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - ListCons - Compiled name 02 - Match``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - ListCons - Compiled name 03 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - ListCons 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - ListCons 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - ListCons 03 - Let``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Null 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - QuoteExpr 01``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Tuple 02 - Struct``() = x.DoNamedTest()

    [<Test>] member x.``Implicit defines 01``() = x.DoNamedTest()
    [<Test; FSharpScriptTest; Explicit>] member x.``Implicit defines 02 - Script``() = x.DoNamedTest()

    [<Test>] member x.``XmlDoc 01 - Simple comment separator``() = x.DoNamedTest()
    [<Test>] member x.``XmlDoc 02 - Simple comment at the end``() = x.DoNamedTest()


[<FSharpSignatureTest>]
type FSharpSignatureParserTest() =
    inherit ParserTestBase<FSharpLanguage>()

    override x.RelativeTestDataPath = "parsing/signatures"

    [<Test>] member x.``Type decl - Union 01 - After nested module``() = x.DoNamedTest()
    [<Test>] member x.``Type decl group 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl group 02 - Attributes``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Union - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union - XmlDoc 02 - No bar``() = x.DoNamedTest()

    [<Test>] member x.``Type decl group - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl group - XmlDoc 02``() = x.DoNamedTest()
    [<Test>] member x.``Type decl group - XmlDoc 03 - With attributes 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl group - XmlDoc 03 - With attributes 02``() = x.DoNamedTest()

    [<Test>] member x.``Val - Value 01``() = x.DoNamedTest()
    [<Test>] member x.``Val - Value 02 - Return attrs``() = x.DoNamedTest()
    [<Test>] member x.``Val - Value 03 - Type func``() = x.DoNamedTest()
    [<Test>] member x.``Val - Value 04 - Literal``() = x.DoNamedTest()

    [<Test>] member x.``Val - Function - Tuple param - Paren 01``() = x.DoNamedTest()
    [<Test>] member x.``Val - Function - Tuple param - Paren 02 - Named``() = x.DoNamedTest()
    [<Test>] member x.``Val - Function - Tuple param - Struct 01``() = x.DoNamedTest()
    [<Test>] member x.``Val - Function - Tuple param - Struct 02 - Named``() = x.DoNamedTest()
    [<Test>] member x.``Val - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Val - Function 02 - Named param``() = x.DoNamedTest()
    [<Test>] member x.``Val - Function 03 - Multiple named params``() = x.DoNamedTest()
    [<Test>] member x.``Val - Function 04 - Named tuple param``() = x.DoNamedTest()
    [<Test>] member x.``Val - Function 05 - Active pattern``() = x.DoNamedTest()

    [<Test>] member x.``Val - Active pattern 01``() = x.DoNamedTest()

    [<Test>] member x.``Val - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Val - XmlDoc 02 - With attributes``() = x.DoNamedTest()

    [<Test>] member x.``Type repr - Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Type repr - Record 02 - Mutable field``() = x.DoNamedTest()
    [<Test>] member x.``Type repr - Record 03 - Field attributes``() = x.DoNamedTest()

    [<Test>] member x.``Type repr - Record - XmlDoc 01``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Inherit 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Inherit 02 - Qualifiers``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Inherit 03 - Generic``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Constructor 01``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Member - Parameter - Attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Parameter - Attribute 02``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Parameter - Attribute 03``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Parameter - Attribute 04``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member - Parameter - Groups 01``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Member 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Member 02 - Parameter 01``() = x.DoNamedTest() // todo: parameter name
    [<Test>] member x.``Type member - Member 03 - Return attribute``() = x.DoNamedTest() // todo: attribute
    [<Test>] member x.``Type member - Member 04 - Tuple``() = x.DoNamedTest() // todo: attribute
    [<Test>] member x.``Type member - Member 05 - Tuple``() = x.DoNamedTest() // todo: attribute

    [<Test>] member x.``Type member - XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - XmlDoc 02 - With attributes``() = x.DoNamedTest()

    [<Test>] member x.``Hash directive 01``() = x.DoNamedTest()


[<FSharpTest>]
type FSharpErrorsParserTest() =
    inherit ParserTestBase<FSharpLanguage>()

    override x.RelativeTestDataPath = "parsing/errors"

    [<Test>] member x.``Expr - Unfinished let 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Unfinished let 02 - In``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Unfinished let 03 - Inline in``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Unfinished let 04 - In, before other``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Unfinished after dot 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Unfinished after dot 02``() = x.DoNamedTest()

    [<Test>] member x.``Line separators 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - CompExpr - Range 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - CompExpr - Range 02 - Step``() = x.DoNamedTest()
    [<Test>] member x.``Expr - CompExpr - Range 03``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Match 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Yield 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Yield - Seq 01``() = x.DoNamedTest() // Parsed as application by FCS
    [<Test>] member x.``Expr - Yield - Seq 02``() = x.DoNamedTest() // Parsed as application by FCS

    [<Test>] member x.``Expr - For 01 - Rarrow``() = x.DoNamedTest()
    [<Test>] member x.``Expr - List - Comprehension 01 - ForExpr``() = x.DoNamedTest()

    [<Test>] member x.``Expr - If 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - If 02``() = x.DoNamedTest()

    [<Test>] member x.``Expr - New 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Expr - Lambda 02 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Interpolated string - Unfinished 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Interpolated string - Unfinished 02``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Object expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Object expr 02``() = x.DoNamedTest()

    [<Test>] member x.``Pat - Char range 01``() = x.DoNamedTest()

    [<Test>] member x.``Module member - Attributes - Expr 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Module member - Attributes 01``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Attributes 02``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Open 01``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Open 02``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Open 03``() = x.DoNamedTest()

    // todo: parse as anon module/namespace?
    [<Test>] member x.``Module member - Expr - Before namespace 01``() = x.DoNamedTest()

    [<Test; Explicit>] member x.``Type - Attribute target 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Type - Attribute target 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Type - Attribute target 03``() = x.DoNamedTest()

    [<Test>] member x.``Type decl - Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Record - Ctor 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Record - Ctor 02 - As``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union - Ctor 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Union - No bar - Attribute 01``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Identifier 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Type member - Identifier 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Type member - Identifier 03``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Identifier 04``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Interface 02``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Interface 03``() = x.DoNamedTest()

    [<Test; FSharpSignatureTest>] member x.``Signature - Id 01``() = x.DoNamedTest()
    [<Test; FSharpSignatureTest>] member x.``Signature - Id 02``() = x.DoNamedTest()
    [<Test; FSharpSignatureTest>] member x.``Signature - Id 03``() = x.DoNamedTest()
