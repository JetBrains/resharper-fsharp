namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveUnusedNamedAsPatTest() =
    inherit FSharpQuickFixTestBase<RemoveUnusedNamedAsPatFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeUnusedNamedAsPat"

    [<Test>] member x.``Function param 01``() = x.DoNamedTest()
    [<Test>] member x.``Let binding 01``() = x.DoNamedTest()
    [<Test>] member x.``Match pattern 01``() = x.DoNamedTest()
