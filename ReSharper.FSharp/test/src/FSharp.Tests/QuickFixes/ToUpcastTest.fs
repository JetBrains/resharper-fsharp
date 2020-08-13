namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ToUpcastTest() =
    inherit FSharpQuickFixTestBase<ToUpcastFix>()

    override x.RelativeTestDataPath = "features/quickFixes/toUpcast"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
