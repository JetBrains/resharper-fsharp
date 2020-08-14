namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ReplaceUseWithLet() =
    inherit FSharpQuickFixTestBase<ReplaceUseWithLetFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceUseWithLet"
    
    [<Test>] member x.``Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Type let binding 01``() = x.DoNamedTest()
