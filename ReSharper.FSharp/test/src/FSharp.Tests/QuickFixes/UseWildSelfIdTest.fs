namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type UseWildSelfIdTest() =
    inherit FSharpQuickFixTestBase<UseWildSelfIdFix>()

    override x.RelativeTestDataPath = "features/quickFixes/useWildSelfId"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Object expression 01``() = x.DoNamedTest()
