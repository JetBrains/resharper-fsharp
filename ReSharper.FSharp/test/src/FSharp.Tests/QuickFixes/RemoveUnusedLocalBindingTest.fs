namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveUnusedLocalBindingTest() =
    inherit FSharpQuickFixTestBase<RemoveUnusedLocalBindingFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeUnusedLocalBinding"

    [<Test>] member x.``Inline 01``() = x.DoNamedTest()
    [<Test>] member x.``Inline 02 - Comment``() = x.DoNamedTest()
    [<Test>] member x.``Inline 03 - In other let``() = x.DoNamedTest()

    [<Test>] member x.``Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 02 - Wrong seq``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 03 - Wrong seq in seq``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 04 - New lines``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 05``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 06 - Nested pattern``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 07 - In other let``() = x.DoNamedTest()

    [<Test>] member x.``Multiple 01``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 02 - Space``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 03 - Space``() = x.DoNamedTest()

    [<Test>] member x.``For 01``() = x.DoNamedTest()
    [<Test>] member x.``For 02 - Space``() = x.DoNamedTest()

    [<Test>] member x.``Match clause 01``() = x.DoNamedTest()

    [<Test>] member x.``Recursive 01``() = x.DoNamedTest()
    [<Test>] member x.``Recursive 02 - Comment after and``() = x.DoNamedTest()
    [<Test>] member x.``Recursive 03 - With binding after``() = x.DoNamedTest()
    [<Test>] member x.``Recursive 04 - First``() = x.DoNamedTest()
    [<Test>] member x.``Recursive 05 - First, more space``() = x.DoNamedTest()
    [<Test>] member x.``Recursive 06 - With binding after, comment``() = x.DoNamedTest()

    [<Test>] member x.``Comp 01``() = x.DoNamedTest()
    [<Test>] member x.``Comp 02 - Inline``() = x.DoNamedTest()
    [<Test>] member x.``Comp 03 - Inline, multiline``() = x.DoNamedTest()

    [<Test>] member x.``Type - Single 01``() = x.DoNamedTest()
    [<Test>] member x.``Type - Single 02``() = x.DoNamedTest()
    [<Test>] member x.``Type - Recursive 01``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Not available 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 02 - Param``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 03 - As``() = x.DoNamedTest()


[<FSharpTest>]
type RemoveUnusedLocalBindingAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/removeUnusedLocalBinding"

    [<Test>] member x.``Text - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Text - Value 01``() = x.DoNamedTest()
