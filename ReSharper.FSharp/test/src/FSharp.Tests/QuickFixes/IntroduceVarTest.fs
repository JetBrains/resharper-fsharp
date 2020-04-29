namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type IntroduceVarTest() =
    inherit QuickFixTestBase<IntroduceVarFix>()

    override x.RelativeTestDataPath = "features/quickFixes/introduceVar"

    [<Test>] member x.``Local - Seq - App 01``() = x.DoNamedTest()
    [<Test>] member x.``Local - Seq - Record 01``() = x.DoNamedTest()

    [<Test>] member x.``Local - Seq - Reference 01``() = x.DoNamedTest()
    [<Test>] member x.``Local - Seq - Reference 02 - Qualified``() = x.DoNamedTest()

    [<Test>] member x.``Local - Seq - Type test 01``() = x.DoNamedTest()

    [<Test>] member x.``Local - Seq - Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``Local - Seq - Multiple occurrences 01``() = x.DoNamedTest()
