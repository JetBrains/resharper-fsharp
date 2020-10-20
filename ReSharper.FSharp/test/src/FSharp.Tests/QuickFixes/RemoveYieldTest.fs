namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages(FSharpCorePackage)>]
type RemoveYieldTest() =
    inherit FSharpQuickFixTestBase<RemoveYieldFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeYield"
    
    [<Test>] member x.``Return 01``() = x.DoNamedTest()
    [<Test>] member x.``Return 02 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Return 03 - New line``() = x.DoNamedTest()

    [<Test>] member x.``Yield 01``() = x.DoNamedTest()
    [<Test>] member x.``Yield 02 - Comment``() = x.DoNamedTest()

    [<Test>] member x.``Return! 01``() = x.DoNamedTest()
    [<Test>] member x.``Yield! 01``() = x.DoNamedTest()
