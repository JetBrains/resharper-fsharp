namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Completion
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
