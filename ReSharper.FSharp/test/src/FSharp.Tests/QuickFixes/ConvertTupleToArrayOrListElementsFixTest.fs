namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type ConvertTupleToArrayOrListElementsFixTest() =
    inherit FSharpQuickFixTestBase<ConvertTupleToArrayOrListElementsFix>()

    override x.RelativeTestDataPath = "features/quickFixes/convertTupleToArrayOrListElements"

    [<Test>] member x.``Array 01``() = x.DoNamedTest()

    [<Test>] member x.``In parens``() = x.DoNamedTest()
    [<Test>] member x.``Large tuple``() = x.DoNamedTest()

    [<Test>] member x.``Multiline 01``() = x.DoNamedTest()


[<FSharpTest>]
type ConvertTupleToElementsTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/convertTupleToArrayOrListElements"

    [<Test>] member x.``Text``() = x.DoNamedTest()
