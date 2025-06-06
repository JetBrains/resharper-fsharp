namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open System.Linq.Expressions
open JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Settings
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.FeaturesTestFramework.Completion
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules
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

    member x.DoNamedTestFsCs() =
        x.DoTestSolution(FSharpTestUtil.referenceCSharpProject x)

    [<Test>] member x.``Attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 02``() = x.DoNamedTest()
    [<Test>] member x.``Attribute - Top level module 01``() = x.DoNamedTest()
    [<Test>] member x.``Attribute - Top level module 02``() = x.DoNamedTest()

    [<Test>] member x.``Basic 01 - Replace``() = x.DoNamedTest()
    [<Test>] member x.``Basic 02 - Insert``() = x.DoNamedTest()
    [<Test>] member x.``Basic 03 - Replace before``() = x.DoNamedTest()
    [<Test>] member x.``Basic 04 - Insert before``() = x.DoNamedTest()
    [<Test>] member x.``Basic 05 - Attribute suffix``() = x.DoNamedTest()
    [<Test>] member x.``Basic 06 - Attribute suffix``() = x.DoNamedTest()

    [<Test; Explicit>] member x.``Bind - Rqa module 01``() = x.DoNamedTest()
    [<Test>] member x.``Bind - Rqa module 02``() = x.DoNamedTest()
    [<Test>] member x.``Bind - Rqa module 03``() = x.DoNamedTest()
    [<Test>] member x.``Bind - Rqa module 04``() = x.DoNamedTest()
    [<Test>] member x.``Bind - Rqa module 05``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Bind - Qualifier - Enum case 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Bind - Qualifier - Enum case 02 - Escape``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Bind - Qualifier - Enum case 03``() = x.DoNamedTest() // todo: fix in 233

    [<Test; Explicit>] member x.``Import - Extension - FSharp - Source - Method - Generic 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Import - Extension - FSharp - Source - Method - Generic 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Import - Extension - FSharp - Source - Method 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Import - Extension - FSharp - Source - Prop 01``() = x.DoNamedTest()
    [<Test>] member x.``Import - Extension 01``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Import - Extension 02``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Import - Extension 03``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Import - Extension 04 - Internal type``() = x.DoNamedTestFsCs()

    [<Test>] member x.``Import - Static member 01``() = x.DoNamedTest()
    [<Test>] member x.``Import - Static member 02``() = x.DoNamedTest()
    [<Test>] member x.``Import - Static member 03``() = x.DoNamedTest()
    [<Test>] member x.``Import - Static member 04``() = x.DoNamedTest()

    [<Test>] member x.``Local val - Binary op 01``() = x.DoNamedTest()
    [<Test>] member x.``Local val - Binary op 02``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 01``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 02``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 03``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 04``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 05``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 06``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 07``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 08``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 09``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 10``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 11``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 12``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 13``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 14``() = x.DoNamedTest()
    [<Test; Explicit "Extra item from FCS">] member x.``Local val - New line 15``() = x.DoNamedTest()
    [<Test; Explicit "Extra item from FCS">] member x.``Local val - New line 16``() = x.DoNamedTest()
    [<Test; Explicit "Extra item from FCS">] member x.``Local val - New line 17``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 18``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 19``() = x.DoNamedTest()
    [<Test>] member x.``Local val - New line 20``() = x.DoNamedTest()
    [<Test>] member x.``Local val - Pattern - Field 01``() = x.DoNamedTest()
    [<Test>] member x.``Local val - Short lambda 01``() = x.DoNamedTest()

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

    [<Test>] member x.``Match - Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Enum 02``() = x.DoNamedTest()
    [<Test>] member x.``Match - Bool 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Bool 02``() = x.DoNamedTest()
    [<Test>] member x.``Match - Bool 03``() = x.DoNamedTest()
    [<Test>] member x.``Match - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Union 02``() = x.DoNamedTest()
    [<Test; Explicit("243")>] member x.``Match - Not available 01``() = x.DoNamedTest()
    [<Test; Explicit("243")>] member x.``Match - Not available 02``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Enum - Matched type 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Pattern - Enum - Rqa 01``() = x.DoNamedTest() // todo: fix in 233
    [<Test>] member x.``Pattern - Enum - Rqa 02 - Nested``() = x.DoNamedTest() // todo: fix in 233
    [<Test>] member x.``Pattern - Enum 01 - Replace qualified``() = x.DoNamedTest() // todo: fix in 233
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

    [<Test>] member x.``Pattern - Union case fields - Escaped 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case fields - Escaped 02 - Rqa``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case fields - Matched type - Or 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case fields - Matched type - Or 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case fields - Matched type - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case fields - Matched type - Tuple 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case fields - Matched type - Tuple 03``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case fields - Not matching type 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union case fields - Qualified 01``() = x.DoNamedTest()

    [<Test>] member x.``Record Field 01``() = x.DoNamedTest()
    [<Test>] member x.``Record Field 02``() = x.DoNamedTest()
    [<Test>] member x.``Record Field 03``() = x.DoNamedTest()
    [<Test>] member x.``Record Field 04``() = x.DoNamedTest()
    [<Test>] member x.``Record Field 05``() = x.DoNamedTest()
    [<Test>] member x.``Record Field 06``() = x.DoNamedTest()

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

    [<Test; Explicit("243")>] member x.``Import - Same project 01``() = x.DoNamedTest()

    [<Explicit "Reuse the common setting">]
    [<Test>] member x.``Import - Same project 02 - Disabled import``() = x.DoNamedTest()

    [<Test>] member x.``Import - Same ns 01``() = x.DoTestFiles("File1.fs", "File2.fs")
    [<Test>] member x.``Import - Same ns 02``() = x.DoTestFiles("File1.fs", "File2.fs")
    [<Test>] member x.``Import - Same ns 03``() = x.DoTestFiles("File1.fs", "File2.fs")
    [<Test; Explicit("243")>] member x.``Import - Same ns 04``() = x.DoTestFiles("File1.fs", "File2.fs")
    [<Test>] member x.``Import - Same ns 05``() = x.DoTestFiles("File1.fs", "File2.fs")

    [<TestReferenceProjectOutput("FSharpClassLib")>]
    [<Test>] member x.``Import - Compiled - FSharp 01``() = x.DoNamedTest()
    [<TestReferenceProjectOutput("FSharpClassLib")>]
    [<Test>] member x.``Import - Compiled - FSharp 02``() = x.DoNamedTest()
    [<TestReferenceProjectOutput("FSharpClassLib")>]
    [<Test>] member x.``Import - Compiled - FSharp 03``() = x.DoNamedTest()
    [<TestReferenceProjectOutput("FSharpClassLib")>]
    [<Test>] member x.``Import - Compiled - FSharp 04``() = x.DoNamedTest()
    [<TestReferenceProjectOutput("FSharpClassLib")>]
    [<Test>] member x.``Import - Compiled - FSharp 05``() = x.DoNamedTest()

    [<Test>] member x.``XmlDoc - tags``() = x.DoNamedTest()

    [<Test>] member x.``Interpolated string 01``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string 02 - Before``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string 02 - After``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string 03 - Start``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string 03 - Middle``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string 03 - End``() = x.DoNamedTest()

    [<Test>] member x.``Override member - ObjExpr - Interface 01`` () = x.DoNamedTest()
    [<Test>] member x.``Override member - ObjExpr 01`` () = x.DoNamedTest()
    [<Test>] member x.``Override member - ObjExpr 02`` () = x.DoNamedTest()
    [<Test>] member x.``Override member - ObjExpr 03`` () = x.DoNamedTest()
    [<Test>] member x.``Override member - ObjExpr 04`` () = x.DoNamedTest()
    [<Test>] member x.``Override member - Property 01`` () = x.DoNamedTest()
    [<Test>] member x.``Override member - Property 02`` () = x.DoNamedTest()
    [<Test>] member x.``Override member - Property 03`` () = x.DoNamedTest()
    [<Test>] member x.``Override member - Property 04`` () = x.DoNamedTest()
    [<Test>] member x.``Override member 01`` () = x.DoNamedTest()
    [<Test>] member x.``Override member 02`` () = x.DoNamedTest()
    [<Test>] member x.``Override member 03`` () = x.DoNamedTest()
    [<Test>] member x.``Override member 04`` () = x.DoNamedTest()
    [<Test>] member x.``Override member 05`` () = x.DoNamedTest()
    [<Test>] member x.``Override member 06`` () = x.DoNamedTest()
    [<Test>] member x.``Override member 07`` () = x.DoNamedTest()
    [<Test>] member x.``Override member 08`` () = x.DoNamedTest()
    [<Test>] member x.``Override member 09`` () = x.DoNamedTest()


[<FSharpTest; FSharpExperimentalFeature(ExperimentalFeature.PostfixTemplates)>]
type FSharpPostfixCompletionTest() =
    inherit CodeCompletionTestBase()

    override x.RelativeTestDataPath = "features/completion/postfix"

    override x.TestType = CodeCompletionTestType.Action

    override this.ExecuteCodeCompletion(suffix, textControl, intellisenseManager, automatic, settingsStore) =
        let occurrenceName = BaseTestWithTextControl.GetSetting(textControl, FSharpTestPopup.OccurrenceName)
        FSharpTestPopup.setOccurrence occurrenceName false this.Solution this.TestLifetime
        base.ExecuteCodeCompletion(suffix, textControl, intellisenseManager, automatic, settingsStore)

    [<Test>] member x.``For - App 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``For - App 02``() = x.DoNamedTest()
    [<Test>] member x.``For - App 03``() = x.DoNamedTest()
    [<Test>] member x.``For - App 04``() = x.DoNamedTest()
    [<Test>] member x.``For - App 05``() = x.DoNamedTest()
    [<Test>] member x.``For - App 06``() = x.DoNamedTest()
    [<Test>] member x.``For - App 07``() = x.DoNamedTest()
    [<Test>] member x.``For - Array - Literal 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Array - Literal 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Array 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Array 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Dictionary 01``() = x.DoNamedTest()
    [<Test>] member x.``For - List - Literal 01``() = x.DoNamedTest()
    [<Test>] member x.``For - List - Literal 02``() = x.DoNamedTest()
    [<Test>] member x.``For - List 01``() = x.DoNamedTest()
    [<Test>] member x.``For - List 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Name 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Not available - Bool 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Not available - Bool 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Not available - Int 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Not available - Int 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Not available - Int 03``() = x.DoNamedTest()
    [<Test>] member x.``For - Not available - Int 04``() = x.DoNamedTest()
    [<Test>] member x.``For - Seq 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Arg 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Arg 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Function return 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Function return 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Function return 03``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Active pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Active pattern 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Constraints 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Constraints 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Constraints 03``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Tuple 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Tuple 03``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Tuple 04``() = x.DoNamedTest()
    [<Test>] member x.``For - Type context - Param - Tuple 05``() = x.DoNamedTest()
    [<Test>] member x.``For - Type parameter 01``() = x.DoNamedTest()
    [<Test>] member x.``For - Type parameter 02``() = x.DoNamedTest()
    [<Test>] member x.``For - Type parameter 03``() = x.DoNamedTest()
    [<Test>] member x.``For - Type parameter 04``() = x.DoNamedTest()
    [<Test>] member x.``For - Type parameter 05 - Eof``() = x.DoNamedTest()

    [<Test>] member x.``Let - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Decl 02``() = x.DoNamedTest()
    [<Test>] member x.``Let - Decl 03``() = x.DoNamedTest()

    [<Test>] member x.``Let - Expr - App 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Expr - App 02``() = x.DoNamedTest()
    [<Test>] member x.``Let - Expr - App 03``() = x.DoNamedTest()
    [<Test>] member x.``Let - Expr - App 04``() = x.DoNamedTest()
    [<Test>] member x.``Let - Expr - Op - Pipe 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Expr - Op - Pipe 02``() = x.DoNamedTest()
    [<Test>] member x.``Let - Expr - Ref 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Expr - Ref 02 - Ctor``() = x.DoNamedTest()
    [<Test>] member x.``Let - Expr 01``() = x.DoNamedTest()

    [<Test>] member x.``Let - Literal - Float 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Literal - Float 02``() = x.DoNamedTest()
    [<Test>] member x.``Let - Literal - Int 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Literal - Int 02``() = x.DoNamedTest()

    [<Test>] member x.``Let - Type - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type - Function 02``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type - KeyValuePair 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type - Named 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Type - Tuple 01``() = x.DoNamedTest()

    [<TestDefines("DEFINE")>]
    [<Test>] member x.``Let - Preprocessor 01``() = x.DoNamedTest()

    [<Test>] member x.``Match - Context - Accessor 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - App 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - App 02``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Auto property 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Binary 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Binary 02``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Binary 03``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Binary 04``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Ctor 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Eof 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Eof 02``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - If 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - If 02``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - If 03``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Lambda 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Lambda 02``() = x.DoNamedTest()
    [<TestSetting(typeof<FSharpFormatSettingsKey>, "MultiLineLambdaClosingNewline", "true")>]
    [<Test>] member x.``Match - Context - Lambda 03``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Let 02``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Let 03``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Let 04``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - List 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - List 02 - For``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Match 02``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Match 03``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Member 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Try 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Context - Try 02``() = x.DoNamedTest()

    [<Test>] member x.``Match - Bool 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Bool 02``() = x.DoNamedTest()
    [<Test>] member x.``Match - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Tuple 02``() = x.DoNamedTest()

    [<Test>] member x.``New 01``() = x.DoNamedTest()

    [<Test>] member x.``Obj - Class - Generic 01``() = x.DoNamedTest()
    [<Test>] member x.``Obj - Class - Generic 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Obj - Class 01``() = x.DoNamedTest()
    [<Test>] member x.``Obj - Class 02``() = x.DoNamedTest()
    [<Test>] member x.``Obj - Class 03``() = x.DoNamedTest()
    [<Test>] member x.``Obj - Interface - Generic 01``() = x.DoNamedTest()
    [<Test>] member x.``Obj - Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Obj - Interface 02``() = x.DoNamedTest()

    [<Test>] member x.``Not available - Let - Open 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Let - Namespace 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Match 01``() = x.DoNamedTest()

    [<Test>] member x.``With 01``() = x.DoNamedTest()
    [<Test>] member x.``With 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``With 03``() = x.DoNamedTest()
    [<Test>] member x.``With 04``() = x.DoNamedTest()
    [<Test>] member x.``With 05``() = x.DoNamedTest()
    [<Test>] member x.``With 06``() = x.DoNamedTest()
    [<Test>] member x.``With 07``() = x.DoNamedTest()
    [<Test>] member x.``With 08``() = x.DoNamedTest()


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

    [<Test; Explicit>] member x.``Constraint 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Constraint 02``() = x.DoNamedTest()

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
    [<Test; Explicit>] member x.``Type - Union 02``() = x.DoNamedTest()
    [<Test>] member x.``Type - Union 03``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Abstract 01``() = x.DoNamedTest()

    // todo: suggest void in extern declarations only, provide info in fcs
    [<Test; Explicit>] member x.``Type member - Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Property 02``() = x.DoNamedTest()

    [<Test>] member x.``Type member 01``() = x.DoNamedTest()

    // todo: add recovery in parser, filter member start keywords
    [<Test; Explicit>] member x.``Type member 02 - Member``() = x.DoNamedTest()

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
    [<Test>] member x.``Expr - Record - Field - Empty 06``() = x.DoNamedTest()
    [<Test; Explicit("243")>] member x.``Expr - Record - Field - Unfinished 01``() = x.DoNamedTest()
    [<Test; Explicit("243")>] member x.``Expr - Record - Field - Unfinished 02``() = x.DoNamedTest()
    [<Test; Explicit("243")>] member x.``Expr - Record - Field - Unfinished 03``() = x.DoNamedTest()
    [<Test; Explicit("243")>] member x.``Expr - Record - Field - Unfinished 04``() = x.DoNamedTest()
    [<Test; Explicit("243")>] member x.``Expr - Record - Field - Unfinished 05 - Another ns``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field - Unfinished 06``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 02 - Other type``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 04``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 05``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 06``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Record - Field 07``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - No reparse ident 01``() = x.DoNamedTest()
    [<Test; Explicit("243")>] member x.``Pattern - Union case - List 01``() = x.DoNamedTest()
    [<Test; Explicit("243")>] member x.``Pattern - Union case - List 02``() = x.DoNamedTest() // todo: fix in 233; types/namespaces are missing
    [<Test>] member x.``Pattern - Union case - List 03``() = x.DoNamedTest() // todo: fix in 233; types/namespaces are missing
    [<Test>] member x.``To recursive - Active pattern 01``() = x.DoNamedTest()

    [<Test; Explicit("243")>] member x.``NamedUnionCaseFieldsPat - 01`` () = x.DoNamedTest()
    [<Test>] member x.``NamedUnionCaseFieldsPat - 02`` () = x.DoNamedTest()
    [<Test>] member x.``NamedUnionCaseFieldsPat - 03`` () = x.DoNamedTest()
    [<Test>] member x.``NamedUnionCaseFieldsPat - 04`` () = x.DoNamedTest()
    [<Test>] member x.``NamedUnionCaseFieldsPat - 05`` () = x.DoNamedTest()
    [<Test>] member x.``NamedUnionCaseFieldsPat - 06`` () = x.DoNamedTest()
    [<Test; Explicit("243")>] member x.``NamedUnionCaseFieldsPat - 07`` () = x.DoNamedTest()
    [<Test; Explicit>] member x.``NamedUnionCaseFieldsPat - 08`` () = x.DoNamedTest()

    override this.BeforeTestStart(_, _, documentText) =
        this.CompleteItem <- FSharpFilteredCompletionTest.GetSetting(documentText, "COMPLETE_ITEM")


[<FSharpTest>]
[<TestReferences("System")>]
type FSharpRegexCompletionTest() =
    inherit CodeCompletionTestBase()

    override x.RelativeTestDataPath = "features/completion/regex"
    override x.TestType = CodeCompletionTestType.Action

    [<Test>] member x.``Non-verbatim string completion 01``() = x.DoNamedTest()

    [<Test>] member x.``Verbatim string completion 01``() = x.DoNamedTest()
    [<Test>] member x.``Verbatim string completion 02``() = x.DoNamedTest()

    [<Test>] member x.``Interpolated string completion 01 - Regular``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string completion 02 - Verbatim``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string completion 03 - Triple quoted``() = x.DoNamedTest()
    [<Test>] member x.``Interpolated string completion 04 - Raw``() = x.DoNamedTest() //TODO: should be available
    [<Test>] member x.``Interpolated string completion 05 - Not available``() = x.DoNamedTest()

    [<Test>] member x.``Brackets 01``() = x.DoNamedTest()
    [<Test>] member x.``Brackets 02 - Interpolation``() = x.DoNamedTest()

    [<Test>] member x.``Active pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Type provider 01``() = x.DoNamedTest()


[<FSharpTest>]
type FSharpCodeCompletionTypingTest() =
    inherit CodeCompletionTypingTestBase<CSharpCompletingCharactersSettingsKey, CSharpAutopopupEnabledSettingsKey>()

    override x.RelativeTestDataPath = "features/completion/typing"

    member this.Quote(e:Expression<System.Func<_, _>>) = e

    override this.GetCompleteOnSpaceSetting() = this.Quote(fun key -> key.CompleteOnSpace)
    override this.GetDoNotCompleteOnSetting() = this.Quote(fun key -> key.NonCompletingCharacters)
    override this.GetAutopopupTypeSetting() = this.Quote(fun key -> key.OnIdent)

    [<Test>] member x.``Space - Pattern - As 01``() = x.DoNamedTest()
    [<Test>] member x.``Space - Pattern - As 02``() = x.DoNamedTest()
    [<Test>] member x.``Space - Pattern - As 03``() = x.DoNamedTest()
    [<Test>] member x.``Space - Pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Space - Pattern 02``() = x.DoNamedTest()
    [<Test; Explicit("dotnet/fsharp#16055")>] member x.``Space - Pattern 03``() = x.DoNamedTest()
    [<Test>] member x.``Space - Pattern 04``() = x.DoNamedTest()

    [<Test>] member x.``Space - Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Space - Record 02``() = x.DoNamedTest()
    [<Test>] member x.``Space - Record 03``() = x.DoNamedTest()
    [<Test>] member x.``Space - Record 04``() = x.DoNamedTest()

    [<Test>] member x.``LParen - Pattern 01``() = x.DoNamedTest() // todo: fix in 233; extra Value item
    [<Test>] member x.``LParen - Pattern 02``() = x.DoNamedTest() // todo: fix in 233; extra Value item
    [<Test>] member x.``LParen - Pattern 03``() = x.DoNamedTest()
    [<Test>] member x.``LParen - Pattern 04``() = x.DoNamedTest() // todo: fix in 233; extra Value item
    
    [<Test>] member x.``Space - NamedUnionCaseField 01`` () = x.DoNamedTest()
    [<Test>] member x.``Space - NamedUnionCaseField 02`` () = x.DoNamedTest()
    [<Test>] member x.``Space - NamedUnionCaseField 03`` () = x.DoNamedTest()


[<FSharpTest>]
type FSharpSelectionCompletionTest() =
    inherit CodeCompletionTestBase()

    override x.RelativeTestDataPath = "features/completion/selection"

    override x.TestType = CodeCompletionTestType.Selection

    [<Test>] member x.``CE - custom operation - 01 prefix`` () = x.DoNamedTest()
    [<Test>] member x.``CE - custom operation - 02 no prefix`` () = x.DoNamedTest()
    [<Test>] member x.``CE - custom operation - 03 for`` () = x.DoNamedTest()
    [<Test>] member x.``CE - custom operation - 04 multiple fors`` () = x.DoNamedTest()
    [<Test>] member x.``CE - custom operation - 05 let`` () = x.DoNamedTest()
    [<Test>] member x.``CE - custom operation - 06 before param`` () = x.DoNamedTest()
    [<Test>] member x.``CE - custom operation - not available - 01 app`` () = x.DoNamedTest()
    [<Test>] member x.``CE - custom operation - not available - 02 if for`` () = x.DoNamedTest()
    [<Test>] member x.``CE - custom operation - not available - 03 use`` () = x.DoNamedTest()
    [<Test>] member x.``CE - custom operation - not available - 04 let nested`` () = x.DoNamedTest()


[<FSharpTest>]
type FSharpCompletionListTest() =
    inherit CodeCompletionTestBase()

    override x.RelativeTestDataPath = "features/completion/list"

    override x.TestType = CodeCompletionTestType.List

    override this.ItemSelector =
            Func<_, _>(fun lookupItem -> lookupItem :? IAspectLookupItem<FSharpNameSuggestionInfo>)

    [<Test>] member x.``Naming - Pat - As - IsInst 01``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat - As - IsInst 02``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat - List - Cons 01``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat - List - Cons 02``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat - List - Cons 03``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Naming - Pat - List - Cons 04``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Naming - Pat - List - Cons 05``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat - List 01``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat - List 02``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat - Or 01``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat - Or 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Naming - Pat - Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat - Tuple 02``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat - Tuple 03``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat 01``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Pat 02``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Type parameter 01``() = x.DoNamedTest()
    [<Test>] member x.``Naming - Type parameter 02``() = x.DoNamedTest()
