namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type RemoveRedundantAttributeSuffixTest() =
    inherit QuickFixTestBase<RemoveRedundantAttributeSuffixFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantAttributeSuffix"

    [<Test>] member x.``Single attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Single attribute 02 - With target and constructor``() = x.DoNamedTest()
    [<Test>] member x.``Multiple attributes 01``() = x.DoNamedTest()
