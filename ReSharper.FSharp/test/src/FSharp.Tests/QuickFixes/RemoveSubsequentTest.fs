namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type RemoveSubsequentTest() =
    inherit QuickFixTestBase<RemoveSubsequentFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeSubsequent"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
