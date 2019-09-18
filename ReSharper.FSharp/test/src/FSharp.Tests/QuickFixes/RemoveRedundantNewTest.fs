namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type RemoveRedundantNewTest() =
    inherit QuickFixTestBase<RemoveRedundantNewFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantNew"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02 - Type args``() = x.DoNamedTest()
