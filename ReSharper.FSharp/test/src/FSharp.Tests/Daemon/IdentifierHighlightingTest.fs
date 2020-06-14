namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open NUnit.Framework
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings

type IdentifierHighlightingTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/identifierHighlighting"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? FSharpIdentifierHighlighting

    [<Test>] member x.``Backticks 01``() = x.DoNamedTest()

    [<Test>] member x.``Operators 01 - ==``() = x.DoNamedTest()
    [<Test>] member x.``Operators 02 - Custom``() = x.DoNamedTest()
    [<Test>] member x.``Operators 03 - Unary``() = x.DoNamedTest()
    [<Test>] member x.``Operators 04 - op_Multiply decl``() = x.DoNamedTest()

    [<Test>] member x.``Active pattern decl``() = x.DoNamedTest()

    [<Test>] member x.``Delegates``() = x.DoNamedTest()

    [<Test>] member x.``Struct constructor``() = x.DoNamedTest()

    [<Test>] member x.``op_RangeStep``() = x.DoNamedTest()

    [<Test>] member x.``Byrefs 01``() = x.DoNamedTest()

    [<Test>] member x.``Union case 01``() = x.DoNamedTest()
    
    [<Test>] member x.``Extension members 01``() = x.DoNamedTest()
    [<Test>] member x.``Extension members 02``() = x.DoNamedTest()
    
    [<Test>] member x.``Functions``() = x.DoNamedTest()
    [<Test>] member x.``Computation expressions``() = x.DoNamedTest()
    
    [<Test>] member x.``Units of measure``() = x.DoNamedTest()