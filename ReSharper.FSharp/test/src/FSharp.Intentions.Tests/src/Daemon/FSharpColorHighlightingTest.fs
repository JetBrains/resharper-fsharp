namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Feature.Services.ColorHints
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestReferences("System.Drawing")>]
type FSharpColorHighlightingTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/colorHighlighting"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? ColorHintHighlighting

    [<Test>] member x.``Methods 01``() = x.DoNamedTest()
    [<Test>] member x.``Methods 02 - Hex``() = x.DoNamedTest()
    [<Test>] member x.``Methods 03 - Octal``() = x.DoNamedTest()
    [<Test>] member x.``Methods 04 - Binary``() = x.DoNamedTest()

    [<Test>] member x.``Properties 01``() = x.DoNamedTest()
