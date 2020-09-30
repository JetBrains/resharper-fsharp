namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveInlineTest() =
    inherit FSharpQuickFixTestBase<RemoveInlineFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeInlineTest"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02 - More space``() = x.DoNamedTest()
