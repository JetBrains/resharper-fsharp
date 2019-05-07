namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open NUnit.Framework

type IdentifierHighlightingTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/identifierHighlighting"

    override x.HighlightingPredicate(_, _, _) = true

    [<Test>] member x.``Backticks 01``() = x.DoNamedTest()

    [<Test>] member x.``Operators 01 - ==``() = x.DoNamedTest()
    [<Test>] member x.``Operators 02 - Custom``() = x.DoNamedTest()

    [<Test>] member x.``Active pattern decl``() = x.DoNamedTest()

    [<Test>] member x.``Delegates``() = x.DoNamedTest()

    [<Test>] member x.``Struct constructor``() = x.DoNamedTest()

    [<Test>] member x.``op_RangeStep``() = x.DoNamedTest()
