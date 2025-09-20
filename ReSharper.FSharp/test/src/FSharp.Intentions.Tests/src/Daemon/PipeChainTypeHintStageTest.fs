namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.TestFramework
open JetBrains.TextControl.DocumentMarkup.Adornments
open NUnit.Framework

[<HighlightOnly(typeof<TypeHintHighlighting>)>]
[<TestSettingsKey(typeof<FSharpTypeHintOptions>)>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowPipeReturnTypes", "true")>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForTopLevelMembers", PushToHintMode.Never)>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForLocalBindings", PushToHintMode.Never)>]
type PipeChainTypeHintStageTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/pipeChainTypeHints"

    [<TestSettings("{HideSameLine:All}")>]
    [<Test>] member x.``Single line 01 - Simple``() = x.DoNamedTest()

    [<TestSettings("{HideSameLine:All}")>]
    [<Test>] member x.``Single line 02 - Multiple``() = x.DoNamedTest()

    [<Test>] member x.``Multi line 01 - One line each``() = x.DoNamedTest()
    [<Test>] member x.``Multi line 02 - Multi line application``() = x.DoNamedTest()

    [<TestSettings("{HideSameLine:All}")>]
    [<Test>] member x.``Multi line 03 - Same line``() = x.DoNamedTest()

    [<Test>] member x.``Multi line 04``() = x.DoNamedTest()
    [<Test>] member x.``Multi line 05 - Last pipe 01``() = x.DoNamedTest()
    [<Test>] member x.``Multi line 06 - Last pipe 02``() = x.DoNamedTest()

    [<Test>] member x.``Error 01 - Invalid reference``() = x.DoNamedTest()
    [<Test>] member x.``Error 02 - Syntax error``() = x.DoNamedTest()
    [<Test>] member x.``Error 03 - Without right argument``() = x.DoNamedTest()
    [<Test>] member x.``Error 04 - Wrong type``() = x.DoNamedTest()

    [<Test>] member x.``Skipped 01 - Other binary op``() = x.DoNamedTest()
    [<Test>] member x.``Skipped 02 - Shadowed pipe op``() = x.DoNamedTest()

    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowPipeReturnTypes", "false")>]
    [<Test>] member x.``Skipped 03 - Setting disabled``() = x.DoNamedTest()
    [<Test>] member x.``Skipped 04 - Unit``() = x.DoNamedTest()
    [<Test>] member x.``Skipped 05 - Inner pipe``() = x.DoNamedTest()

    [<Test>] member x.``Display context 01``() = x.DoNamedTest()
