namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddParensToApplicationTest() =
    inherit FSharpQuickFixTestBase<AddParensToApplicationFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addParensToApplication"

    [<Test>] member x.``Single application``() = x.DoNamedTest()
    [<Test>] member x.``Multiply applications``() = x.DoNamedTest()
    [<Test>] member x.``Lambda expression 1``() = x.DoNamedTest()
    [<Test>] member x.``Lambda expression 2``() = x.DoNamedTest()
    [<Test>] member x.``Curried function``() = x.DoNamedTest()
    [<Test>] member x.``Curried function with parens``() = x.DoNamedTest()
    [<Test>] member x.``Application inside application``() = x.DoNamedTest()
    [<Test>] member x.``First arg is application``() = x.DoNamedTest()
    [<Test>] member x.``Application with not enough args``() = x.DoNamedTest()
    [<Test>] member x.``Long ident application``() = x.DoNamedTest()
    [<Test>] member x.``Type constructor 01 - Union case``() = x.DoNamedTest()
    [<Test>] member x.``Type constructor 02 - Exception``() = x.DoNamedTest()
    [<Test>] member x.``Type constructor 03 - With lambda application``() = x.DoNamedTest()


[<FSharpTest>]
type AddParensToApplicationAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/addParensToApplication"

    [<Test>] member x.``Single application - reference``() = x.DoNamedTest()
    [<Test>] member x.``Single application - lambda``() = x.DoNamedTest()
    [<Test>] member x.``Multiply applications text``() = x.DoNamedTest()
