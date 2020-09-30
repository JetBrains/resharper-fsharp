namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages("FSharp.Core")>]
type ToAbstractFixTest() =
    inherit FSharpQuickFixTestBase<ToAbstractFix>()

    override x.RelativeTestDataPath = "features/quickFixes/toAbstract"

    [<Test>] member x.``Abstract member 01``() = x.DoNamedTest()
    [<Test>] member x.``Base type 01``() = x.DoNamedTest()
