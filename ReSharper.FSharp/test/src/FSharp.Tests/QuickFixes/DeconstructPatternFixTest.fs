namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

type DeconstructPatternFixTest() =
    inherit FSharpQuickFixTestBase<DeconstructPatternFix>()

    override x.RelativeTestDataPath = "features/quickFixes/deconstruct"

    [<Test>] member x.``Union case fields 01``() = x.DoNamedTest()
