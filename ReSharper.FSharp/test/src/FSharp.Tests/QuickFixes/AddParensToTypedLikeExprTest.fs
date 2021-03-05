namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddParensToTypedLikeExprTest() =
    inherit FSharpQuickFixTestBase<AddParensToTypedLikeExprFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addParensToTypedLikeExpr"

    [<Test>] member x.``Type test 01``() = x.DoNamedTest()
    [<Test>] member x.``Type test 02``() = x.DoNamedTest()
    [<Test>] member x.``Upcast 01``() = x.DoNamedTest()


[<FSharpTest>]
type AddParensToTypedLikeExprAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/addParensToTypedLikeExpr"

    [<Test>] member x.``Text - Type test``() = x.DoNamedTest()
    [<Test>] member x.``Text - Type cast``() = x.DoNamedTest()

    [<Test>] member x.``Availability 01 - Not available``() = x.DoNamedTest()
    [<Test>] member x.``Availability 02``() = x.DoNamedTest()
