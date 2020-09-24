namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveRedundantAttributeSuffixTest() =
    inherit FSharpQuickFixTestBase<RemoveRedundantAttributeSuffixFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantAttributeSuffix"

    [<Test>] member x.``Single attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Single attribute 02 - With target and constructor``() = x.DoNamedTest()
    [<Test>] member x.``Multiple attributes 01``() = x.DoNamedTest()
