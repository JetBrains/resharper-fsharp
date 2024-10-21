namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.TestFramework
open JetBrains.TextControl.DocumentMarkup.Adornments
open NUnit.Framework

[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForTopLevelMembers", PushToHintMode.Never)>]
type InferredTypeCodeVisionProviderTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/inferredTypeCodeVision"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? FSharpInferredTypeHighlighting

    [<Test>] member x.``Module functions and values``() = x.DoNamedTest()
    [<Test>] member x.``Type fields and members``() = x.DoNamedTest()
    [<Test>] member x.``Unopened namespace``() = x.DoNamedTest()

    [<Test>] member x.``Object expression 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param``() = x.DoNamedTest()

    [<Test>] member x.``Binding - As 01``() = x.DoNamedTest()
    [<Test>] member x.``Binding - As 02 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Binding - Paren 01``() = x.DoNamedTest()
