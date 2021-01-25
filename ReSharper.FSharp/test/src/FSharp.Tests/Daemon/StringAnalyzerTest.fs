namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Daemon.StringAnalysis
open NUnit.Framework

type StringAnalyzerTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/strings"
    
    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? StringEscapeCharacterHighlighting


    [<Test>] member x.``String escaping 01 - Strings``() = x.DoNamedTest()
    [<Test>] member x.``String escaping 02 - Chars``() = x.DoNamedTest()

    [<Test>] member x.``Regular - Interpolated 01``() = x.DoNamedTest()
    [<Test>] member x.``Regular - Interpolated 02``() = x.DoNamedTest()
    [<Test>] member x.``Regular - Interpolated 03``() = x.DoNamedTest()
    [<Test>] member x.``Triple quote - Interpolated 01``() = x.DoNamedTest()
    [<Test>] member x.``Triple quote - Interpolated 02``() = x.DoNamedTest()
    [<Test>] member x.``Triple quote 01``() = x.DoNamedTest()
    [<Test>] member x.``Verbatim - Interpolated 01``() = x.DoNamedTest()
    [<Test>] member x.``Verbatim - Interpolated 02``() = x.DoNamedTest()
    [<Test>] member x.``Verbatim - Interpolated 03``() = x.DoNamedTest()
    [<Test>] member x.``Verbatim - Interpolated 04``() = x.DoNamedTest()
    [<Test>] member x.``Verbatim 01``() = x.DoNamedTest()
    [<Test>] member x.``Verbatim 02 - Byte array``() = x.DoNamedTest()
