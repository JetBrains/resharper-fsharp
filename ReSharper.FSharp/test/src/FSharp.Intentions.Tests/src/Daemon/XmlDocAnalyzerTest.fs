namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon
open NUnit.Framework

type XmlDocAnalyzerTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/xmlDoc"

    override x.HighlightingPredicate(highlighting, _, _) =
        match highlighting with
        | :? InvalidXmlDocPositionWarning -> true
        | _ -> false

    [<Test>] member x.``Invalid XmlDoc position 01``() = x.DoNamedTest()
    [<Test>] member x.``Invalid XmlDoc position 02 - Not available``() = x.DoNamedTest()
