namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest; AssertCorrectTreeStructure>]
type ToAbstractFixTest() =
    inherit FSharpQuickFixTestBase<ToAbstractFix>()

    override x.RelativeTestDataPath = "features/quickFixes/toAbstract"

    [<Test>] member x.``Abstract member 01``() = x.DoNamedTest()
    [<Test>] member x.``Base type 01``() = x.DoNamedTest()
