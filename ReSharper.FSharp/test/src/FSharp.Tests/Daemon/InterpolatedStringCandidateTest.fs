namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages(FSharpCorePackage); HighlightOnly(typeof<InterpolatedStringCandidateWarning>)>]
type InterpolatedStringCandidateTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/interpolatedStringCandidate"

    [<Test>] member x.``Simple 01 - sprintf``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02 - printfn``() = x.DoNamedTest()
    [<Test>] member x.``Simple 03 - Parens``() = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp47)>]
    [<Test>] member x.``Not available 01 - Language version``() = x.DoNamedTest()
    [<Test>] member x.``Not available 02 - Too many args``() = x.DoNamedTest()
    [<Test>] member x.``Not available 03 - Too few args``() = x.DoNamedTest()
    [<Test>] member x.``Not available 04 - Applied byte array``() = x.DoNamedTest()
    [<Test>] member x.``Not available 05 - Multi arg format specifier``() = x.DoNamedTest()
    [<Test>] member x.``Not available 06 - Multi arg format specifier wrong arg count``() = x.DoNamedTest()
    [<Test>] member x.``Not available 07 - Other string``() = x.DoNamedTest()
    [<Test>] member x.``Not available 08 - Interpolated string``() = x.DoNamedTest()
    [<Test>] member x.``Not available 09 - Interpolated verbatim string``() = x.DoNamedTest()
    [<Test>] member x.``Not available 10 - Applied triple quoted string``() = x.DoNamedTest()
    [<Test>] member x.``Not available 11 - Applied triple quoted string on triple quoted format``() = x.DoNamedTest()
    [<Test>] member x.``Not available 12 - Applied string literal``() = x.DoNamedTest()
    [<Test>] member x.``Not available 13 - Descendant string in applied arg``() = x.DoNamedTest()
