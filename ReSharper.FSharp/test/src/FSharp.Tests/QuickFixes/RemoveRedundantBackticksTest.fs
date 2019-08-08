namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type RemoveRedundantBackticksTest() =
    inherit QuickFixTestBase<RemoveRedundantBackticksFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantBackticks"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Keyword 01``() = x.DoNamedTest()
