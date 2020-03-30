namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open NUnit.Framework

type PipeChainCodeVisionProviderTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/pipeChainCodeVision"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? FSharpPipeChainHighlighting

    [<Test>] member x.``Multi line``() = x.DoNamedTest()
