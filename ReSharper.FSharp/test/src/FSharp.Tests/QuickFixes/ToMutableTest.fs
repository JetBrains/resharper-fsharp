namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

// todo: add test with signature files

[<FSharpTest; TestPackages("FSharp.Core")>]
type ToMutableRecordFieldFixTest() =
    inherit QuickFixTestBase<ToMutableRecordFieldFix>()

    override x.RelativeTestDataPath = "features/quickFixes/toMutable"

    [<Test>] member x.``Record field 01``() = x.DoNamedTest()
    [<Test>] member x.``Record field 02 - Attributes``() = x.DoNamedTest()
