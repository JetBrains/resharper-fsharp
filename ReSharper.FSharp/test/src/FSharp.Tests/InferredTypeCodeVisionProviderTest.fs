namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Daemon.CodeInsights
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open NUnit.Framework

type InferredTypeCodeVisionProviderTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/inferredTypeCodeVision"

    override x.HighlightingPredicate(highlighting, _, _) =
        match highlighting with
        | :? CodeInsightsHighlighting as x -> x.Provider :? InferredTypeCodeVisionProviderProcess
        | _ -> false

    [<Test>] member x.``Common``() = x.DoNamedTest()
