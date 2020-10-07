namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages("FSharp.Core")>]
type IntroduceVarTest() =
    inherit FSharpQuickFixTestBase<IntroduceVarFix>()

    override x.RelativeTestDataPath = "features/quickFixes/introduceVar"

    [<Test>] member x.``Local - Seq - App 01``() = x.DoNamedTest()
    [<Test>] member x.``Local - Seq - Record 01``() = x.DoNamedTest()

    [<Test>] member x.``Local - Seq - Reference 01``() = x.DoNamedTest()
    [<Test>] member x.``Local - Seq - Reference 02 - Qualified``() = x.DoNamedTest()

    [<Test>] member x.``Local - Seq - Type test 01``() = x.DoNamedTest()

    [<Test>] member x.``Local - Seq - Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``Local - Seq - Multiple occurrences 01``() = x.DoNamedTest()

    [<Test>] member x.``Local - Seq - Last 01``() = x.DoNamedTest()

    [<Test>] member x.``Do 01``() = x.DoNamedTest()
    [<Test>] member x.``For 01``() = x.DoNamedTest()
    [<Test>] member x.``If 01``() = x.DoNamedTest()

    [<Test>] member x.``Module - Do 01 - Implicit``() = x.DoNamedTest()
    [<Test>] member x.``Module - Do 02 - Function``() = x.DoNamedTest()
    [<Test>] member x.``Module - Do 03 - Function``() = x.DoNamedTest()
