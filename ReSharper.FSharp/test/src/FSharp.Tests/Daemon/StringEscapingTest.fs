namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Daemon.StringAnalysis
open NUnit.Framework

type StringEscapingTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/stringEscaping"
    
    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? StringEscapeCharacterHighlighting


    [<Test>] member x.``String escaping 01 - Strings``() = x.DoNamedTest()
    [<Test>] member x.``String escaping 02 - Chars``() = x.DoNamedTest()
