namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveRedundantQualifierTest() =
    inherit FSharpQuickFixTestBase<RemoveRedundantQualifierFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantQualifier"

    [<Test>] member x.``Reference expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Reference name 01``() = x.DoNamedTest()
    [<Test>] member x.``Type extension 01``() = x.DoNamedTest()

    [<Test; ExecuteScopedActionInFile>] member x.``Multiple 01``() = x.DoNamedTest()
