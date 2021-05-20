namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
[<TestSettingsKey(typeof<FSharpFormatSettingsKey>)>]
[<TestSettings("{AllowHighPrecedenceAppParens:All}")>]
type RedundantParenExprTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/redundantParens/expr"

    override x.DoTest(lifetime, project) =
        use cookie = FSharpExperimentalFeatures.EnableRedundantParenAnalysisCookie.Create()
        base.DoTest(lifetime, project)

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? RedundantParenExprWarning
    
    [<Test>] member x.``Active pattern 01``() = x.DoNamedTest()

    [<Test>] member x.``Literal 01``() = x.DoNamedTest()
    [<Test>] member x.``Literal 02 - Qualified``() = x.DoNamedTest()

    [<Test>] member x.``App - Local 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Top level 01``() = x.DoNamedTest()

    [<Test>] member x.``App - Precedence - High 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Precedence - High 02 - Multiple``() = x.DoNamedTest()
    [<Test>] member x.``App - Precedence - High 03 - Multiple - Last``() = x.DoNamedTest()

    [<Test>] member x.``App - Precedence - Low 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Precedence - Low 02``() = x.DoNamedTest()

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

    [<Test>] member x.``Binary - App 01``() = x.DoNamedTest()
    [<Test>] member x.``Binary - App 02 - If``() = x.DoNamedTest()
    [<Test>] member x.``Binary - Op deindent 01``() = x.DoNamedTest()
    [<Test>] member x.``Binary - Typed 01``() = x.DoNamedTest()

    [<Test>] member x.``For each - Condition - If 01``() = x.DoNamedTest()

    [<Test>] member x.``If - Condition - Seq 01``() = x.DoNamedTest()
    [<Test>] member x.``If - Condition - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``If - Condition - Type cast 01``() = x.DoNamedTest()
    [<Test>] member x.``If - Condition - Type check 01``() = x.DoNamedTest()

    [<Test>] member x.``Let - Local - App - Binary 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local - App - Binary 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Let - Local - Literal 01``() = x.DoNamedTest()

    [<Test>] member x.``Let - Top - App - Binary 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Top - App - Binary 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Let - Top - Literal 01``() = x.DoNamedTest()

    [<Test>] member x.``Match - Expr - Seq 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Expr - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Expr - Type cast 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Expr - Type check 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Clause expr - App 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Clause expr - App 02``() = x.DoNamedTest()
    [<Test>] member x.``Match - Clause expr - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Clause expr - Seq - Different indent 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Clause expr - Seq - Different indent 02 - Deindent``() = x.DoNamedTest()
    [<Test>] member x.``Match - Clause expr - Seq 01``() = x.DoNamedTest()
    [<Test>] member x.``Match - Clause expr - Seq 02 - Semicolon``() = x.DoNamedTest()

    [<Test>] member x.``New - App 01``() = x.DoNamedTest()

    [<Test>] member x.``Paren 01``() = x.DoNamedTest()
    [<Test>] member x.``Paren 02``() = x.DoNamedTest()

    [<Test>] member x.``Required - Inherit 01``() = x.DoNamedTest()
    [<Test>] member x.``Required - Inherit 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Required - New expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Required - Obj expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Required - Record - Inherit 01``() = x.DoNamedTest()

    [<Test>] member x.``Seq - Binary - Deindent 01``() = x.DoNamedTest()
    [<Test>] member x.``Seq - Fun - Deindent 01``() = x.DoNamedTest()

    [<Test>] member x.``Tuple - App 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Interpolated string``() = x.DoNamedTest()

    [<Test>] member x.``When - Binary 01``() = x.DoNamedTest()
    [<Test>] member x.``When - Binary 02 - Pipe``() = x.DoNamedTest()
    [<Test>] member x.``When - If - Binary 01``() = x.DoNamedTest() // todo: parens
    [<Test>] member x.``When - If - Seq 01``() = x.DoNamedTest()
    [<Test>] member x.``When - If - Seq 02 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``When - Let 01``() = x.DoNamedTest()
    [<Test>] member x.``When - Typed 01``() = x.DoNamedTest()
    [<Test>] member x.``When - Typed 02``() = x.DoNamedTest()
    [<Test>] member x.``When - Typed 03 - Record``() = x.DoNamedTest()
    [<Test>] member x.``When 01``() = x.DoNamedTest()

    [<Test>] member x.``While - Condition - Seq 01``() = x.DoNamedTest()
    [<Test>] member x.``While - Condition - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``While - Condition - Type cast 01``() = x.DoNamedTest()
    [<Test>] member x.``While - Condition - Type check 01``() = x.DoNamedTest()

