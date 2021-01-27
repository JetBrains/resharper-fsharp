namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Highlighting
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type HighlightUsagesTest() =
    inherit HighlightActionTestBase()

    override x.RelativeTestDataPath = "actions/highlightUsages"
    override this.HighlightActionName = "HighlightUsages"

    [<Test>] member x.``Let declaration 01``() = x.DoNamedTest()
    [<Test>] member x.``Let declaration 02 - Union case repr``() = x.DoNamedTest()
