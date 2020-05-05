namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type ToUpcastTest() =
    inherit QuickFixTestBase<ToUpcastFix>()

    override x.RelativeTestDataPath = "features/quickFixes/toUpcast"

    [<Test; ExpectErrors 3198>] member x.``Simple 01``() = x.DoNamedTest()
