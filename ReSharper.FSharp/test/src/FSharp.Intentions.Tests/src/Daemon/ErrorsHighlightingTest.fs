namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Daemon.SyntaxHighlighting
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type ErrorsHighlightingTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/errorsHighlighting"

    [<Test>] member x.``Enum Rqa analyzer 01``() = x.DoNamedTest()

    [<Test>] member x.``Extension analyzer``() = x.DoNamedTest()

    [<HighlightOnly(typeof<RedundantIndexerDotWarning>)>]
    [<Test>] member x.``Indexer dot analyzer 01``() = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp50)>]
    [<HighlightOnly(typeof<RedundantIndexerDotWarning>)>]
    [<Test>] member x.``Indexer dot analyzer 02``() = x.DoNamedTest()

    [<HighlightOnly(typeof<RedundantStringInterpolationWarning>, typeof<ReSharperSyntaxHighlighting>)>]
    [<Test>] member x.``Interpolated string 01``() = x.DoNamedTest()

    [<Test>] member x.``ListConsPat analyzer - Empty list tail 01``() = x.DoNamedTest()

    [<Test>] member x.``Redundant as pat 01``() = x.DoNamedTest()
    [<Test>] member x.``Redundant as pat 02 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Redundant union case pattern - Active pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Redundant union case pattern - Nested 01``() = x.DoNamedTest()

    [<Test>] member x.``Self id 01``() = x.DoNamedTest()
    [<Test>] member x.``Self id 02 - Property with accessors``() = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp46)>]
    [<Test>] member x.``Self id - Not available 01``() = x.DoNamedTest()

    [<HighlightOnly(typeof<InvalidXmlDocPositionWarning>)>]
    [<Test>] member x.``Invalid XmlDoc position 01``() = x.DoNamedTest()

    [<HighlightOnly(typeof<InvalidXmlDocPositionWarning>)>]
    [<Test>] member x.``Invalid XmlDoc position 02 - Not available``() = x.DoNamedTest()

    [<TestCustomInspectionSeverity(InvalidXmlDocPositionWarningHighlightingId, Severity.ERROR)>]
    [<HighlightOnly(typeof<InvalidXmlDocPositionWarning>)>]
    [<Test>] member x.``Invalid XmlDoc position 03 - As error``() = x.DoNamedTest()
