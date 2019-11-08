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
    [<Test>] member x.``Simple 02 - Multiple subsequent``() = x.DoNamedTest()
    [<Test>] member x.``Simple 03 - Not the first``() = x.DoNamedTest()
    [<Test>] member x.``Simple 04 - Comment``() = x.DoNamedTest()
    [<Test>] member x.``Simple 05 - Comment``() = x.DoNamedTest()
    [<Test>] member x.``Simple 06 - Single line``() = x.DoNamedTest()
    [<Test>] member x.``Simple 07 - Semicolon``() = x.DoNamedTest()
    [<Test>] member x.``Simple 08 - Semicolon``() = x.DoNamedTest()
