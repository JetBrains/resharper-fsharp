namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.TestFramework
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open NUnit.Framework

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

    [<TestReferences("System")>]
    [<Test>] member x.``Delegates 01``() = x.DoNamedTest()
    
    [<TestReferences("System")>]
    [<Test>] member x.``Delegates 02``() = x.DoNamedTest()

    [<TestReferences("System")>]
    [<Test>] member x.``Delegates 03``() = x.DoNamedTest()
    
    [<Test>] member x.``Struct constructor``() = x.DoNamedTest()

    [<Test>] member x.``op_RangeStep``() = x.DoNamedTest()

    [<Test>] member x.``Byrefs 01``() = x.DoNamedTest()

    [<Test>] member x.``Union case 01``() = x.DoNamedTest()

    [<Test>] member x.``Extension members 01``() = x.DoNamedTest()

    [<TestReferences("System", "System.Core")>]
    [<Test>] member x.``Extension members 02``() = x.DoNamedTest()    

    [<TestReferences("System")>]
    [<Test>] member x.``Functions 01``() = x.DoNamedTest()

    [<Test>] member x.``Functions 02``() = x.DoNamedTest()

    [<Test>] member x.``Computation expressions``() = x.DoNamedTest()

    [<Test>] member x.``Units of measure``() = x.DoNamedTest()

    [<TestReferences("System")>]
    [<Test>] member x.``Type aliases 01 - Simple types with System dependence``() = x.DoNamedTest()
    [<Test>] member x.``Type aliases 02 - Simple types without System dependence``() = x.DoNamedTest()
    [<Test>] member x.``Type aliases 03 - Units of Measure``() = x.DoNamedTest()
    [<Test>] member x.``Type aliases 04 - Core abbreviations``() = x.DoNamedTest()

    [<Test>] member x.``Method - Application 01``() = x.DoNamedTest()
    [<Test>] member x.``Method - Parameters - Optional 01``() = x.DoNamedTest()
    [<Test>] member x.``Method - Parameters - Optional 02 - Private``() = x.DoNamedTest()
