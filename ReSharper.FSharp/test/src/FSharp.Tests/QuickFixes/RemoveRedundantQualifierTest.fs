namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages(FSharpCorePackage)>]
type RemoveRedundantQualifierTest() =
    inherit FSharpQuickFixTestBase<RemoveRedundantQualifierFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantQualifier"

    [<Test>] member x.``Reference expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Reference name 01``() = x.DoNamedTest()
    [<Test>] member x.``Type extension 01``() = x.DoNamedTest()

    [<Test; ExecuteScopedQuickFixInFile>] member x.``Multiple 01``() = x.DoNamedTest()
