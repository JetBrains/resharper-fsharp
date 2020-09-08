namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open NUnit.Framework

type SimplifyListConsPatTest() =
    inherit FSharpQuickFixTestBase<SimplifyListConsPatFix>()

    override x.RelativeTestDataPath = "features/quickFixes/simplifyListConsPat"

    [<Test>] member x.``Test 01``() = x.DoNamedTest()
    [<Test>] member x.``Test 02 - Space``() = x.DoNamedTest()
    [<Test>] member x.``Test 03 - Comment``() = x.DoNamedTest()
