namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddParensToApplicationTest() =
    inherit QuickFixTestBase<AddParensToApplicationFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addParensToApplication"

    [<Test>] member x.``Single application``() = x.DoNamedTest()
    [<Test>] member x.``Multiply applications``() = x.DoNamedTest()
    [<Test>] member x.``Lambda expression 1``() = x.DoNamedTest()
    [<Test>] member x.``Lambda expression 2``() = x.DoNamedTest()
    [<Test>] member x.``Curried function``() = x.DoNamedTest()
    [<Test>] member x.``Curried function with parens``() = x.DoNamedTest()
    [<Test>] member x.``Application inside application``() = x.DoNamedTest()
    [<Test>] member x.``First arg is application``() = x.DoNamedTest()

[<FSharpTest>]
type AddParensToApplicationAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/addParensToApplication"

    [<Test>] member x.``Not enough arguments - not available``() = x.DoNamedTest()
    [<Test>] member x.``Without applications - not available``() = x.DoNamedTest()
    [<Test>] member x.``Display prefix app name``() = x.DoNamedTest()
    [<Test>] member x.``Display curried app name``() = x.DoNamedTest()
    [<Test>] member x.``Display lambda name``() = x.DoNamedTest()
