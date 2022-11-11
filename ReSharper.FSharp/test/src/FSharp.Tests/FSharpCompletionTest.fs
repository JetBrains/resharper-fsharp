namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open JetBrains.ReSharper.FeaturesTestFramework.Completion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpCompletionTest() =
    inherit CodeCompletionTestBase()

    override x.RelativeTestDataPath = "features/completion"

    override x.TestType = CodeCompletionTestType.Action

    member x.DoTestFiles([<ParamArray>] names: string[]) =
        let testDir = x.TestDataPath / x.TestMethodName
        let paths = names |> Array.map (fun name -> testDir.Combine(name).FullPath)
        x.DoTestSolution(paths)

    [<Test>] member x.``Attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 02``() = x.DoNamedTest()

    [<Test>] member x.``Basic 01 - Replace``() = x.DoNamedTest()
    [<Test>] member x.``Basic 02 - Insert``() = x.DoNamedTest()
    [<Test>] member x.``Basic 03 - Replace before``() = x.DoNamedTest()
    [<Test>] member x.``Basic 04 - Insert before``() = x.DoNamedTest()
    [<Test>] member x.``Basic 05 - Attribute suffix``() = x.DoNamedTest()
    [<Test>] member x.``Basic 06 - Attribute suffix``() = x.DoNamedTest()

    [<Test>] member x.``Bind - Rqa module 01``() = x.DoNamedTest()
    [<Test>] member x.``Bind - Rqa module 02``() = x.DoNamedTest()
    [<Test>] member x.``Bind - Rqa module 03``() = x.DoNamedTest()
    [<Test>] member x.``Bind - Rqa module 04``() = x.DoNamedTest()
    [<Test>] member x.``Bind - Qualifier - Enum case 01``() = x.DoNamedTest()
    [<Test>] member x.``Bind - Qualifier - Enum case 02 - Escape``() = x.DoNamedTest()

    [<Test>] member x.``Local val - Binary op 01``() = x.DoNamedTest()
    [<Test>] member x.``Local val - Binary op 02``() = x.DoNamedTest()
    [<Test; Explicit("Needs reparse")>] member x.``Local val - New line 01``() = x.DoNamedTest()
    [<Test; Explicit("Needs reparse")>] member x.``Local val - New line 02``() = x.DoNamedTest()

    [<Test; Explicit>] member x.``Lambda - Arg - Curried - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Arg - Curried - Tuple 01 - First``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Arg - Curried - Tuple 02 - Last``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Arg - Curried 01``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Arg - Curried 02``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Arg - Curried 03``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Pipe - Double 01``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Pipe 01``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Pipe 02``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Pipe 03 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Enum - Matched type 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Enum - Rqa 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Enum - Rqa 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Enum 01 - Replace qualified``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Escaped 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Escaped 02 - Rqa``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Import 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Import 02 - Rqa``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Import 03 - Rqa module``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Import 04 - Rqa module``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Import 05 - Rqa and nested``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Matched type - Or 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Matched type - Or 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Matched type - Tuple - Paren 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Matched type - Tuple - Paren 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Matched type - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Matched type - Tuple 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Matched type - Tuple 03``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Matched type - Tuple 04``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Matched type - Tuple 05``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Matched type - Tuple 06``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - No Fields 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - No Fields 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - No fields 03 - Rqa``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Not matching type 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Qualified - Wrong type 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Qualified - Wrong type 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Qualified 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Qualified 02``() = x.DoNamedTest()
    [<Test; Explicit("Fcs completion fails")>] member x.``Pattern - Union case - Qualified 03``() = x.DoNamedTest()
    [<Test; Explicit("Fcs completion fails")>] member x.``Pattern - Union case - Qualified 04``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Union case - Rqa 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Rqa 02 - Obj expr``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case - Rqa 03 - Extension``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Union case 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case 03``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case 04 - Rqa``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case 05``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case 06``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case 07``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case 08 - Module Rqa``() = x.DoNamedTest()
    [<TestSetting(typeof<FSharpFormatSettingsKey>, "SpaceBeforeUppercaseInvocation", "true")>]
    [<Test>] member x.``Pattern - Union case 09 - Space``() = x.DoNamedTest()

    [<Test>] member x.``To recursive - Escape 01``() = x.DoNamedTest()
    [<Test>] member x.``To recursive - Local 01``() = x.DoNamedTest()
    [<Test>] member x.``To recursive - Local 02``() = x.DoNamedTest()
    [<Test>] member x.``To recursive - Top level 01``() = x.DoNamedTest()
    [<Test>] member x.``To recursive - Top level 02``() = x.DoNamedTest()

    [<Test>] member x.``Qualified 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualified 02``() = x.DoNamedTest()
    [<Test>] member x.``Qualified 03 - Eof``() = x.DoNamedTest()
    [<Test>] member x.``Qualified 04 - Space``() = x.DoNamedTest()
    [<Test>] member x.``Qualified 05``() = x.DoNamedTest()
    [<Test>] member x.``Qualified 06``() = x.DoNamedTest()
    [<Test>] member x.``Qualified 07 - Enum``() = x.DoNamedTest()

    [<Test>] member x.``Wild 01 - Replace``() = x.DoNamedTest()
    [<Test>] member x.``Wild 02 - Insert``() = x.DoNamedTest()
    [<Test>] member x.``Wild 03 - Replace before``() = x.DoNamedTest()
    [<Test>] member x.``Wild 04 - Insert before``() = x.DoNamedTest()

    [<Test>] member x.``Open 01 - First open``() = x.DoNamedTest()
    [<Test>] member x.``Open 02 - Second open``() = x.DoNamedTest()
    [<Test>] member x.``Open 03 - Comment after namespace``() = x.DoNamedTest()

    [<TestSetting(typeof<FSharpOptions>, "TopLevelOpenCompletion", "false")>]
    [<Test>] member x.``Open 04 - Inside module``() = x.DoNamedTest()

    [<TestSetting(typeof<FSharpOptions>, "TopLevelOpenCompletion", "false")>]
    [<Test>] member x.``Open 06 - Inside module, space``() = x.DoNamedTest()

    [<Test>] member x.``Open 07 - After System``() = x.DoNamedTest()
    [<Test>] member x.``Open 08 - Before other System``() = x.DoNamedTest()

    [<TestSetting(typeof<FSharpOptions>, "TopLevelOpenCompletion", "false")>]
    [<Test>] member x.``Open - Indent - Nested - After 01``() = x.DoNamedTest()

    [<TestSetting(typeof<FSharpOptions>, "TopLevelOpenCompletion", "false")>]
    [<Test>] member x.``Open - Indent - Nested - Before 01``() = x.DoNamedTest()

    [<Test>] member x.``Open - Indent - Top - After 01``() = x.DoNamedTest()
    [<Test>] member x.``Open - Indent - Top - Before 01``() = x.DoNamedTest()

    [<Test>] member x.``Import - Anon module 01 - First line``() = x.DoNamedTest()
    [<Test>] member x.``Import - Anon module 02 - Before open``() = x.DoNamedTest()
    [<Test>] member x.``Import - Anon module 03 - After open``() = x.DoNamedTest()
    [<Test>] member x.``Import - Anon module 04 - After comment``() = x.DoNamedTest()

    [<Test>] member x.``Import - Sibling namespace``() = x.DoNamedTest()

    [<Test>] member x.``Import - Same project 01``() = x.DoNamedTest()

    [<TestSetting(typeof<FSharpOptions>, "EnableOutOfScopeCompletion", "false")>]
    [<Test>] member x.``Import - Same project 02 - Disabled import``() = x.DoNamedTest()

    [<Test>] member x.``Import - Same ns 01``() = x.DoTestFiles("File1.fs", "File2.fs")
    [<Test>] member x.``Import - Same ns 02``() = x.DoTestFiles("File1.fs", "File2.fs")
    [<Test>] member x.``Import - Same ns 03``() = x.DoTestFiles("File1.fs", "File2.fs")
    [<Test>] member x.``Import - Same ns 04``() = x.DoTestFiles("File1.fs", "File2.fs")
    [<Test>] member x.``Import - Same ns 05``() = x.DoTestFiles("File1.fs", "File2.fs")

    [<Test>] member x.``XmlDoc - tags``() = x.DoNamedTest()

    [<Test>] member x.``Interpolated string 01``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string 02 - Before``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string 02 - After``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string 03 - Start``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string 03 - Middle``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string 03 - End``() = x.DoNamedTest()

[<FSharpTest>]
type FSharpPostfixCompletionTest() =
    inherit CodeCompletionTestBase()

    override x.RelativeTestDataPath = "features/completion/postfix"

    override x.TestType = CodeCompletionTestType.Action

    [<Test>] member x.``For - Array - Literal 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Array - Literal 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Array 01``() = x.DoNamedTest()
    [<Test>] member x.``For - List - Literal 01``() = x.DoNamedTest()
    [<Test>] member x.``For - List - Literal 02``() = x.DoNamedTest()
    [<Test>] member x.``For - List 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Not available - Bool 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Not available - Bool 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Not available - Int 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Not available - Int 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``For - Type context - Arg 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``For - Type context - Arg 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``For - Type context - Function return 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``For - Type context - Function return 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``For - Type context - Function return 03``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``For - Type context - Param - Active pattern 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``For - Type context - Param - Active pattern 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Constraints 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``For - Type context - Param - Constraints 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Tuple 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``For - Type context - Param - Tuple 03``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Tuple 04``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``For - Type context - Param - Tuple 05``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``For - Type context - Param 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Type parameter 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Type parameter 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Type parameter 03``() = x.DoNamedTest()

    [<Test>] member x.``Let - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Decl 02``() = x.DoNamedTest()
    [<Test>] member x.``Let - Decl 03``() = x.DoNamedTest()

    [<Test>] member x.``Let - Expr - Op - Pipe 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Expr - Ref 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Expr - Ref 02 - Ctor``() = x.DoNamedTest()
    [<Test>] member x.``Let - Expr 01``() = x.DoNamedTest()

    [<Test>] member x.``Let - Literal - Float 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Literal - Float 02``() = x.DoNamedTest()
    [<Test>] member x.``Let - Literal - Int 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Literal - Int 02``() = x.DoNamedTest()

    [<Test>] member x.``Let - Type - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type - Function 02``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type - Named 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type - Tuple 01``() = x.DoNamedTest()

    [<TestDefines("DEFINE")>]
    [<Test>] member x.``Let - Preprocessor 01``() = x.DoNamedTest()

    [<Test>] member x.``Not available - Let - Open 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Let - Namespace 01``() = x.DoNamedTest()


[<AbstractClass; FSharpTest>]
type FSharpKeywordCompletionTestBase() =
    inherit CodeCompletionTestBase()

    override x.TestType = CodeCompletionTestType.List

    override this.ItemSelector =
        Func<_, _>(function :? FSharpKeywordLookupItem as keyword -> keyword.IsReparseContextAware | _ -> false)

type FSharpKeywordCompletionTest() =
    inherit FSharpKeywordCompletionTestBase()

    override x.RelativeTestDataPath = "features/completion/keywords"

    [<Test>] member x.``Anon module - Expr - Before module 01``() = x.DoNamedTest()
    [<Test>] member x.``Anon module - Expr - Before module 02 - Nested``() = x.DoNamedTest()

    // todo: check node parent is file
    [<Test>] member x.``Anon module - Expr - Before namespace 01``() = x.DoNamedTest()

    [<Test; Explicit("Can't get tree node")>] member x.``Anon module - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Anon module - Expr 02 - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Anon module - Expr 03 - After another``() = x.DoNamedTest()

    [<Test>] member x.``Const 01``() = x.DoNamedTest()
    [<Test>] member x.``Const 02``() = x.DoNamedTest()
    [<Test>] member x.``Const 03``() = x.DoNamedTest()
    [<Test>] member x.``Const 04``() = x.DoNamedTest()
    [<Test>] member x.``Const 05``() = x.DoNamedTest()
    [<Test>] member x.``Const 06``() = x.DoNamedTest()

    [<Test>] member x.``Constraint 01``() = x.DoNamedTest()
    [<Test>] member x.``Constraint 02``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Comp - App - List ctor 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - App - List ctor 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - App 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - App 02 - List``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - App 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - App 04``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - Let - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - Let - In expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - Let - In expr 02 - App``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - List ctor 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - Ref 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - Ref 02 - Qualifier``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - Ref 03 - Qualifier``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Comp - Seq 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Do - Ref 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Seq 01``() = x.DoNamedTest()

    [<Test>] member x.``Module member - Before type 01``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Before type 02``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Module abbreviation 01``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Namespace 01``() = x.DoNamedTest()
    [<Test>] member x.``Module member 01``() = x.DoNamedTest()

    [<Test>] member x.``Open 01``() = x.DoNamedTest()
    [<Test>] member x.``Open 02``() = x.DoNamedTest()
    [<Test>] member x.``Open 03``() = x.DoNamedTest()
    [<Test>] member x.``Open 04``() = x.DoNamedTest()
    // todo: add recovery in parser, filter member start keywords
    [<Test>] member x.``Open 05``() = x.DoNamedTest()

    [<Test>] member x.``Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Type - Attribute target 01``() = x.DoNamedTest()
    [<Test; Explicit("Can't reparse")>] member x.``Type - Attribute target 02``() = x.DoNamedTest()
    [<Test; Explicit("Can't reparse")>] member x.``Type - Attribute target 03``() = x.DoNamedTest()
    [<Test>] member x.``Type - Delegate 01``() = x.DoNamedTest()
    [<Test>] member x.``Type - Delegate 02``() = x.DoNamedTest()
    [<Test>] member x.``Type - Exception 01``() = x.DoNamedTest()
    [<Test>] member x.``Type - Exception 02``() = x.DoNamedTest()
    [<Test>] member x.``Type - Exception 03``() = x.DoNamedTest()
    [<Test>] member x.``Type - Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Type - Union 02``() = x.DoNamedTest()
    [<Test>] member x.``Type - Union 03``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Abstract 01``() = x.DoNamedTest()

    // todo: suggest void in extern declarations only, provide info in fcs
    [<Test>] member x.``Type member - Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Property 02``() = x.DoNamedTest()

    [<Test>] member x.``Type member 01``() = x.DoNamedTest()

    // todo: add recovery in parser, filter member start keywords
    [<Test>] member x.``Type member 02 - Member``() = x.DoNamedTest()

    [<Explicit("Get non-parsed identifier from reparse context")>]
    [<Test>] member x.``Module - Top level 03 - Before namespace``() = x.DoNamedTest()

    [<Explicit("Get non-parsed identifier from reparse context")>]
    [<Test>] member x.``Namespace - Top level 03 - Before namespace``() = x.DoNamedTest()

[<FSharpSignatureTest>]
type FSharpSignatureKeywordCompletionTest() =
    inherit FSharpKeywordCompletionTestBase()

    override x.RelativeTestDataPath = "features/completion/keywords/signatures"

    [<Test>] member x.``Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Module member - Module abbreviation 01``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member 01``() = x.DoNamedTest()


[<FSharpTest>]
type FSharpFilteredCompletionTest() =
    inherit CodeCompletionTestBase()

    override x.RelativeTestDataPath = "features/completion/filtered"

    override x.TestType = CodeCompletionTestType.List

    member val CompleteItem = null with get, set

    override this.ItemSelector =
        Func<_, _>(fun lookupItem ->
            isNull this.CompleteItem ||

            match lookupItem with
            | :? FcsLookupItem as item -> item.Text = this.CompleteItem
            | _ -> lookupItem.DisplayName.Text = this.CompleteItem)

    [<Test>] member x.``Expr - Base 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Base 02 - Local``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Record - Field - Empty 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field - Empty 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field - Empty 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field - Empty 04``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field - Empty 05 - Another ns``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field - Unfinished 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field - Unfinished 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field - Unfinished 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field - Unfinished 04``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field - Unfinished 05 - Another ns``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 02 - Other type``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 04``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 05``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 06``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 07``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - No reparse ident 01``() = x.DoNamedTest()
    [<Test>] member x.``To recursive - Active pattern 01``() = x.DoNamedTest()

    override this.BeforeTestStart(_, _, documentText) =
        this.CompleteItem <- FSharpFilteredCompletionTest.GetSetting(documentText, "COMPLETE_ITEM")
