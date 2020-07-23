namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages("FSharp.Core")>]
type RedundantParensTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/redundantParens"

    override x.DoTest(lifetime, project) =
        use cookie = FSharpRegistryUtil.AllowExperimentalFeaturesCookie.Create()
        base.DoTest(lifetime, project)

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? RedundantParenExprWarning
    
    [<Test>] member x.``Literals 01``() = x.DoNamedTest()

    [<Test>] member x.``App - Local 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Top level 01``() = x.DoNamedTest()

    [<Test>] member x.``App - Precedence - High 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Precedence - High 02 - Multiple``() = x.DoNamedTest()
    [<Test>] member x.``App - Precedence - High 03 - Multiple - Last``() = x.DoNamedTest()

    [<Test>] member x.``App - Precedence - Low 01``() = x.DoNamedTest()

    [<Test>] member x.``App - Precedence - List 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Precedence - List 02``() = x.DoNamedTest()
    [<Test>] member x.``App - Precedence - List 03``() = x.DoNamedTest()

    [<Test>] member x.``App - Precedence - Indexer 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Precedence - Indexer 02 - Multiple``() = x.DoNamedTest()

    [<Test>] member x.``Arg - High precedence 01``() = x.DoNamedTest()
    [<Test>] member x.``Arg - High precedence 02 - Member``() = x.DoNamedTest()

    [<Test>] member x.``Arg - Low precedence 01``() = x.DoNamedTest()
    [<Test>] member x.``Arg - Low precedence 02 - Member``() = x.DoNamedTest()

    [<Test>] member x.``App - Attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Attribute 02 - Type function``() = x.DoNamedTest()
    [<Test>] member x.``App - Attribute 03 - Reference``() = x.DoNamedTest()
    [<Test>] member x.``App - Attribute 04 - Targets``() = x.DoNamedTest()

    [<Test>] member x.``Let - Local - App - Binary 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local - App - Binary 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local - Literal 01``() = x.DoNamedTest()

    [<Test>] member x.``Let - Top - App - Binary 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Top - App - Binary 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Let - Top - Literal 01``() = x.DoNamedTest()

    [<Test>] member x.``Required - Inherit 01``() = x.DoNamedTest()
    [<Test>] member x.``Required - Inherit 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Required - New expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Required - Obj expr 01``() = x.DoNamedTest()
