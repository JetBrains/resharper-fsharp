namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<HighlightOnly(typeof<TypeHintHighlighting>)>]
[<TestPackages("FSharp.Core")>]
[<TestSettingsKey(typeof<FSharpTypeHintOptions>)>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowPipeReturnTypes", "false")>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowInferredTypes", "true")>]
type InferredTypeHintStageTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/inferredTypeHints"

    [<Test>] member x.``Function 01 - No arguments``() = x.DoNamedTest()
    [<Test>] member x.``Function 02 - Curried arguments``() = x.DoNamedTest()
    [<Test>] member x.``Function 03 - Generic arguments``() = x.DoNamedTest()
    [<Test>] member x.``Function 04 - Tupled arguments``() = x.DoNamedTest()
    [<Test>] member x.``Function 05 - Function arguments``() = x.DoNamedTest()
    [<Test>] member x.``Function 06 - Type annotated - Curried``() = x.DoNamedTest()
    [<Test>] member x.``Function 07 - Type annotated - Tupled``() = x.DoNamedTest()
    [<Test>] member x.``Function 08 - Local function``() = x.DoNamedTest()

    [<Test>] member x.``Pattern match 01 - Option``() = x.DoNamedTest()
    [<Test>] member x.``Pattern match 02 - Record``() = x.DoNamedTest()
    [<Test>] member x.``Pattern match 03 - Wildcard``() = x.DoNamedTest()
