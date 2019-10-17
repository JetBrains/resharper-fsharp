namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type RemoveUnusedLocalBindingTest() =
    inherit QuickFixTestBase<RemoveUnusedLocalBindingFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeUnusedLocalBinding"

    [<Test>] member x.``Inline 01``() = x.DoNamedTest()
    [<Test>] member x.``Inline 02 - Comment``() = x.DoNamedTest()

    [<Test>] member x.``Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 02 - Wrong seq``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 03 - Wrong seq in seq``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 04 - New lines``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 05``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 06 - Nested pattern``() = x.DoNamedTest()

    [<Test>] member x.``Recursive 01``() = x.DoNamedTest()
    [<Test>] member x.``Recursive 02 - Comment after and``() = x.DoNamedTest()
    [<Test>] member x.``Recursive 03 - With binding after``() = x.DoNamedTest()
    [<Test>] member x.``Recursive 04 - First``() = x.DoNamedTest()
    [<Test>] member x.``Recursive 05 - First, more space``() = x.DoNamedTest()
    [<Test>] member x.``Recursive 06 - With binding after, comment``() = x.DoNamedTest()


[<FSharpTest>]
type RemoveUnusedLocalBindingAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/removeUnusedLocalBinding"

    [<Test>] member x.``Not available 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available 02 - Param``() = x.DoNamedTest()

    [<Test>] member x.``Text - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Text - Value 01``() = x.DoNamedTest()
