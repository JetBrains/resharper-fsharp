namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type AddIgnoreTest() =
    inherit FSharpQuickFixTestBase<AddIgnoreFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addIgnore"

    [<Test>] member x.``Module 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Module 02 - App``() = x.DoNamedTest()
    [<Test>] member x.``Module 03 - Multiline``() = x.DoNamedTest()

    [<Test>] member x.``Expression 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expression 02 - App``() = x.DoNamedTest()
    [<Test>] member x.``Expression 03 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Expression 04 - Lazy``() = x.DoNamedTest()
    [<Test>] member x.``Expression 05 - &&``() = x.DoNamedTest()
    [<Test>] member x.``Expression 06 - Same precedence``() = x.DoNamedTest()
    [<Test>] member x.``Expression 07 - Same precedence``() = x.DoNamedTest()
    [<Test>] member x.``Expression 08 - Qualifier app``() = x.DoNamedTest()
    [<Test>] member x.``Expression 09 - Qualifier app``() = x.DoNamedTest()

    [<Test>] member x.``New line - Lazy 01``() = x.DoNamedTest()

    [<TestBulbActionMenuSelectedIndexes([|1|])>]
    [<Test>] member x.``New line - Match - Deindent - Single clause 01``() = x.DoNamedTest()
    [<Test>] member x.``New line - Match - Deindent - Single clause 02``() = x.DoNamedTest()
    [<TestBulbActionMenuSelectedIndexes([|1|])>]
    [<Test>] member x.``New line - Match - Deindent 01``() = x.DoNamedTest()
    [<TestBulbActionMenuSelectedIndexes([|1|])>]
    [<Test>] member x.``New line - Match - Deindent 02``() = x.DoNamedTest()
    [<Test>] member x.``New line - Match - Deindent 03``() = x.DoNamedTest()

    [<TestBulbActionMenuSelectedIndexes([|1|])>]
    [<Test>] member x.``New line - Match 01``() = x.DoNamedTest()
    [<TestBulbActionMenuSelectedIndexes([|1|])>]
    [<Test>] member x.``New line - Match 02``() = x.DoNamedTest()
    [<TestBulbActionMenuSelectedIndexes([|1|])>]
    [<Test>] member x.``New line - Match 03 - Single line``() = x.DoNamedTest()

    [<Test>] member x.``Parens 01``() = x.DoNamedTest()
    [<Test>] member x.``Parens 02``() = x.DoNamedTest()

    [<TestBulbActionMenuSelectedIndexes([|1|])>]
    [<Test>] member x.``Single line - Multiple clauses 01``() = x.DoNamedTest()
    [<Test>] member x.``Single line - Multiple clauses 02``() = x.DoNamedTest()
    [<TestBulbActionMenuSelectedIndexes([|1|])>]
    [<Test>] member x.``Single line - Single clause 01``() = x.DoNamedTest()
    [<Test>] member x.``Single line - Single clause 02``() = x.DoNamedTest()

    [<Test>] member x.``Inner expression - If 01``() = x.DoNamedTest()
    [<Test>] member x.``Inner expression - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Inner expression - Try 01 - With``() = x.DoNamedTest()
    [<Test>] member x.``Inner expression - Try 02 - Finally``() = x.DoNamedTest()

    [<Test>] member x.``Missing else branch 01``() = x.DoNamedTest()


[<FSharpTest>]
type AddIgnoreAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/addIgnore"

    [<Test>] member x.``Availability - Unexpected expression type``() = x.DoNamedTest()
    [<Test>] member x.``Availability - If expression wrong type``() = x.DoNamedTest()
    [<Test>] member x.``Availability - Else branch wrong type``() = x.DoNamedTest()
