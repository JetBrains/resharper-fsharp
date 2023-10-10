namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon
open NUnit.Framework

type RecordExprAnalyzerTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/recordExprAnalyzer"

    override x.HighlightingPredicate(highlighting, _, _) =
        match highlighting with
        | :? NestedRecordUpdateCanBeSimplifiedWarning -> true
        | _ -> false

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp80)>]
    [<Test>] member x.Availability() = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp80)>]
    [<Test>] member x.``Availability - Overlap``() = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp70)>]
    [<Test>] member x.``Availability - F# 7``() = x.DoNamedTest()
