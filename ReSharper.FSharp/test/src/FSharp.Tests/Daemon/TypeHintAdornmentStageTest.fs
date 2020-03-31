namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<HighlightOnly(typeof<TypeHintHighlighting>)>]
[<TestPackages("FSharp.Core")>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowPipeReturnTypes", "true")>]
type TypeHintAdornmentStageTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/typeHintAdornment"

    [<Test>] member x.``Single line 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Single line 02 - Multiple``() = x.DoNamedTest()

    [<Test>] member x.``Multi line 01 - One line each``() = x.DoNamedTest()
    [<Test>] member x.``Multi line 02 - Multi line application``() = x.DoNamedTest()

    [<Test>] member x.``Error 01 - Invalid reference``() = x.DoNamedTest()
    [<Test>] member x.``Error 02 - Syntax error``() = x.DoNamedTest()

    [<Test>] member x.``Skipped 01 - Other binary op``() = x.DoNamedTest()
    [<Test>] member x.``Skipped 02 - Shadowed pipe op``() = x.DoNamedTest()

    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowPipeReturnTypes", "false")>]
    [<Test>] member x.``Skipped 03 - Setting disabled``() = x.DoNamedTest()
