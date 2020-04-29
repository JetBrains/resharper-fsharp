namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Daemon.VisualElements
open JetBrains.ReSharper.Feature.Services.ParameterNameHints
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestReferences("System.Drawing")>]
type ParameterNameHintStageTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/parameterNameHint"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? ParameterNameHintHighlighting

    [<Test>] member x.``App 01 - Multiple literals``() = x.DoNamedTest()
    [<Test>] member x.``App 02 - Some literals``() = x.DoNamedTest()
    [<Test>] member x.``App 03 - Parentheses``() = x.DoNamedTest()
    [<Test>] member x.``App 04 - Ignore binary eq``() = x.DoNamedTest()
    [<Test>] member x.``App 05 - After constructor``() = x.DoNamedTest()
    [<Test>] member x.``App 06 - Ignored``() = x.DoNamedTest()
    [<Test>] member x.``Constructor 01``() = x.DoNamedTest()
