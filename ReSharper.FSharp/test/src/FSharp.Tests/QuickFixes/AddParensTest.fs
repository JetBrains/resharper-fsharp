namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddParensTest() =
    inherit FSharpQuickFixTestBase<AddParensFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addParens"

    [<Test>] member x.``Single line``() = x.DoNamedTest()
    [<Test>] member x.``Multi line``() = x.DoNamedTest()
    [<Test>] member x.``Successive qualifiers``() = x.DoNamedTest()
    [<Test>] member x.``Qualifier app``() = x.DoNamedTest()
