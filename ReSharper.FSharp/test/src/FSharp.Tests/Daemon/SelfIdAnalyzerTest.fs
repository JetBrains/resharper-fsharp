namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open NUnit.Framework

type SelfIdAnalyzerTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/selfId"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp46)>]
    [<Test>] member x.``Not available 01 - Language version``() = x.DoNamedTest()
