namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages(FSharpCorePackage)>]
type RemoveUnusedOpensTest() =
    inherit FSharpQuickFixTestBase<RemoveUnusedOpensFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeUnusedOpens"
    
    [<Test>] member x.``Single open``() = x.DoNamedTest()
    [<Test>] member x.``Multiple opens``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Single open with semicolon``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Multiple opens with semicolon``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Multiple semicolons and spaces``() = x.DoNamedTest()
    [<Test>] member x.``Multiple unused and used opens``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Open with line comment``() = x.DoNamedTest()
