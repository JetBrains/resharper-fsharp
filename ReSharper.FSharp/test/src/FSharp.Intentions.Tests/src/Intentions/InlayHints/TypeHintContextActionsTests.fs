namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.InlayHints

open JetBrains.Diagnostics
open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open JetBrains.TextControl.DocumentMarkup.Adornments
open NUnit.Framework

type ActionNotAvailable() =
    inherit ExpectedExceptionInsideSolutionAttribute(ExpectedMessage = nameof(ActionNotAvailable))

[<FSharpTest; AssertCorrectTreeStructure>]
[<TestSettingsKey(typeof<FSharpTypeHintOptions>)>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForTopLevelMembers", PushToHintMode.Always)>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForLocalBindings", PushToHintMode.Always)>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowForOtherPatterns", PushToHintMode.Always)>]
type TypeHintContextActionsTests() =
    inherit InlayHintContextMenuTestBase<TypeHintHighlighting>()

    override x.RelativeTestDataPath = "features/intentions/inlayHints/types"

    [<Test>] member x.``Parameter 01``() = x.DoNamedTest()
    [<Test>] member x.``Parameter 02 - Optional``() = x.DoNamedTest()

    [<Test>] member x.``Lambda 01``() = x.DoNamedTest()

    [<Test>] member x.``Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 02 - Top level``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 03 - As``() = x.DoNamedTest()

    //TODO: remove parens
    [<Test>] member x.``Record field 01``() = x.DoNamedTest()

    [<Test>] member x.``Union case 01``() = x.DoNamedTest()
    //TODO: remove parens
    [<Test>] member x.``Union case 02 - Named``() = x.DoNamedTest()
    [<Test>] member x.``Union case 03 - Parens``() = x.DoNamedTest()

     //TODO: remove parens
    [<Test>] member x.``As pat 01``() = x.DoNamedTest()
    [<Test>] member x.``Ands pat 01``() = x.DoNamedTest()

    [<Test>] member x.``Return type 01 - Function``() = x.DoNamedTest()
    [<Test>] member x.``Return type 02 - Method``() = x.DoNamedTest()
    [<Test>] member x.``Return type 03 - Property``() = x.DoNamedTest()
    [<Test>] member x.``Return type 04 - Binding``() = x.DoNamedTest()

    [<Test>] member x.``Match 01 - Array``() = x.DoNamedTest()
    [<Test>] member x.``Match 02 - List 01``() = x.DoNamedTest()
    [<Test>] member x.``Match 03 - List 02``() = x.DoNamedTest()
    [<Test>] member x.``Match 04 - List 03``() = x.DoNamedTest()

    [<Test; ActionNotAvailable>] member x.``Not available 01 - Let bang``() = x.DoNamedTest()
    [<Test; ActionNotAvailable>] member x.``Not available 02 - Property with accessors``() = x.DoNamedTest()

    override x.IsAvailable(item) = item.RichText.Text = "Add type annotation"
    override x.OnQuickFixNotAvailable(_, _) = Assertion.Fail(nameof(ActionNotAvailable))
