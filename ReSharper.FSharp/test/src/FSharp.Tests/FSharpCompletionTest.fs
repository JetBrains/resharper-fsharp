namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open JetBrains.ReSharper.FeaturesTestFramework.Completion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpCompletionTest() =
    inherit CodeCompletionTestBase()

    override x.RelativeTestDataPath = "features/completion"

    override x.TestType = CodeCompletionTestType.Action

    [<Test>] member x.``Basic 01 - Replace``() = x.DoNamedTest()
    [<Test>] member x.``Basic 02 - Insert``() = x.DoNamedTest()
    [<Test>] member x.``Basic 03 - Replace before``() = x.DoNamedTest()
    [<Test>] member x.``Basic 04 - Insert before``() = x.DoNamedTest()

    [<Test>] member x.``Bind - Qualifier - Enum case 01``() = x.DoNamedTest()
    [<Test>] member x.``Bind - Qualifier - Enum case 02 - Escape``() = x.DoNamedTest()

    [<Test>] member x.``Local val - Binary op 01``() = x.DoNamedTest()
    [<Test>] member x.``Local val - Binary op 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Local val - New line 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Local val - New line 02``() = x.DoNamedTest()

    [<Test; Explicit>] member x.``Lambda - Arg - Curried - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Arg - Curried - Tuple 01 - First``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Arg - Curried - Tuple 02 - Last``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Arg - Curried 01``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Arg - Curried 02``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Arg - Curried 03``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Pipe - Double 01``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Pipe 01``() = x.DoNamedTest()
    [<Test>] member x.``Lambda - Pipe 02``() = x.DoNamedTest()

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

[<FSharpTest>]
type FSharpPostfixCompletionTest() =
    inherit CodeCompletionTestBase()

    override x.RelativeTestDataPath = "features/completion/postfix"

    override x.TestType = CodeCompletionTestType.Action

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

    [<Test>] member x.``Not available - Let - Open 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Let - Namespace 01``() = x.DoNamedTest()

[<FSharpTest>]
type FSharpKeywordCompletionTest() =
    inherit CodeCompletionTestBase()

    override x.RelativeTestDataPath = "features/completion/keywords"

    override x.TestType = CodeCompletionTestType.Action

    override this.ItemSelector =
        Func<_, _>(function :? FSharpKeywordLookupItem as keyword -> keyword.IsReparseContextAware | _ -> false)

    [<Test>] member x.``AndBang - Not available 01``() = x.DoNamedTest()
    [<Test>] member x.``AndBang - Not available 02``() = x.DoNamedTest()
    [<Test>] member x.``AndBang - Not available 03``() = x.DoNamedTest()
    [<Test>] member x.``AndBang 01``() = x.DoNamedTest()
    [<Test>] member x.``AndBang 02``() = x.DoNamedTest()

    [<Test>] member x.``Extern 01``() = x.DoNamedTest()
    [<Test>] member x.``Extern 02 - Attributes``() = x.DoNamedTest()

    [<Test>] member x.``Module - Top level 01 - Before module``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top level 02 - Before nested module``() = x.DoNamedTest()

    [<Explicit("Get non-parsed identifier from reparse context")>]
    [<Test>] member x.``Module - Top level 03 - Before namespace``() = x.DoNamedTest()

    [<Test>] member x.``Module 01 - Module``() = x.DoNamedTest()
    [<Test>] member x.``Module 02 - Anon module``() = x.DoNamedTest()
    [<Test>] member x.``Module 03 - Namespace``() = x.DoNamedTest()

    [<Test>] member x.``Namespace - Top level 01 - Before module``() = x.DoNamedTest()
    [<Test>] member x.``Namespace - Top level 02 - Before nested module``() = x.DoNamedTest()

    [<Explicit("Get non-parsed identifier from reparse context")>]
    [<Test>] member x.``Namespace - Top level 03 - Before namespace``() = x.DoNamedTest()

    [<Test>] member x.``Namespace 01 - Module``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 02 - Anon module``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 03 - Namespace``() = x.DoNamedTest()

    [<Test>] member x.``Open - Not available - Expression 01``() = x.DoNamedTest()
    [<Test>] member x.``Open - Not available - Type member 01``() = x.DoNamedTest()
    [<Test>] member x.``Open 01 - Module``() = x.DoNamedTest()
    [<Test>] member x.``Open 02 - Anon module``() = x.DoNamedTest()
    [<Test>] member x.``Open 03 - Before type``() = x.DoNamedTest()
    [<Test>] member x.``Open 04 - At type``() = x.DoNamedTest()
    [<Test>] member x.``Open 05 - Module abbreviation``() = x.DoNamedTest()

    [<Test; Explicit>] member x.``Type - Attribute target 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Type - Attribute target 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Type - Attribute target 03``() = x.DoNamedTest()
    [<Test>] member x.``Type - Module member 01``() = x.DoNamedTest()
    [<Test>] member x.``Type - Module member 02 - Module abbreviation``() = x.DoNamedTest()
    [<Test>] member x.``Type - Module member 03 - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Type - Open 01``() = x.DoNamedTest()
    [<Test>] member x.``Type - Open 02``() = x.DoNamedTest()
    [<Test>] member x.``Type - Open 03``() = x.DoNamedTest()
    [<Test>] member x.``Type - Open 04``() = x.DoNamedTest()

    [<Test>] member x.``YieldBang - Not available - Binary app 01``() = x.DoNamedTest()
    [<Test>] member x.``YieldBang - Not available - Binary app 02``() = x.DoNamedTest()
    [<Test>] member x.``YieldBang - Not available - Binary app 03``() = x.DoNamedTest()
    [<Test>] member x.``YieldBang - Not available - Prefix app 01``() = x.DoNamedTest()
    [<Test>] member x.``YieldBang - Not available 01``() = x.DoNamedTest()
    [<Test>] member x.``YieldBang 01``() = x.DoNamedTest()
    [<Test>] member x.``YieldBang 02``() = x.DoNamedTest()
    [<Test>] member x.``YieldBang 03``() = x.DoNamedTest()
    [<Test>] member x.``YieldBang 04``() = x.DoNamedTest()
