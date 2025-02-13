namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.TestFramework
open JetBrains.TextControl.DocumentMarkup.Adornments
open JetBrains.TextControl.DocumentMarkup.Adornments.IntraTextAdornments
open NUnit.Framework

[<HighlightOnly(typeof<TypeHintHighlighting>)>]
[<TestSettingsKey(typeof<FSharpTypeHintOptions>)>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForTopLevelMembers", PushToHintMode.Always)>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForLocalBindings", PushToHintMode.Always)>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowForOtherPatterns", PushToHintMode.Always)>]
type TypeHintStageTest() =
    inherit FSharpHighlightingTestBase()

    let [<Literal>] PatternsCommonTestFile = "_SettingsTestSource.fs"
    override x.RelativeTestDataPath = "features/daemon/typeHints"

    [<Test>] member x.``Settings 01 - Show all``() = x.DoSettingsTest()

    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForLocalBindings", PushToHintMode.Never)>]
    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowForOtherPatterns", PushToHintMode.Never)>]
    [<Test>] member x.``Settings 02 - Top level``() = x.DoSettingsTest()

    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForTopLevelMembers", PushToHintMode.Never)>]
    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowForOtherPatterns", PushToHintMode.Never)>]
    [<Test>] member x.``Settings 03 - Locals``() = x.DoSettingsTest()

    [<TestSetting(typeof<GeneralInlayHintsOptions>, "EnableInlayHints", false)>]
    [<Test>] member x.``Settings 04 - Disabled 01``() = x.DoSettingsTest()

    [<TestSetting(typeof<GeneralInlayHintsOptions>, "DefaultMode", PushToHintMode.Never)>]
    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForTopLevelMembers", PushToHintMode.Default)>]
    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForLocalBindings", PushToHintMode.Default)>]
    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowForOtherPatterns", PushToHintMode.Default)>]
    [<Test>] member x.``Settings 05 - Disabled 02 - By mode``() = x.DoSettingsTest()

    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForTopLevelMembers", PushToHintMode.Never)>]
    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForLocalBindings", PushToHintMode.Never)>]
    [<Test>] member x.``Settings 06 - Other patterns``() = x.DoSettingsTest()

    [<Test>] member x.``Patterns 01``() = x.DoNamedTest()
    [<Test>] member x.``Patterns 02 - Other``() = x.DoNamedTest()

    [<Test>] member x.``Unfinished declarations and expressions 01``() = x.DoNamedTest()

    member x.DoSettingsTest() = x.DoTestSolution(PatternsCommonTestFile)

    override x.GetGoldTestDataPath(fileName) =
        if fileName = PatternsCommonTestFile then
            x.GetTestDataFilePath2(x.TestMethodName + ".fs.gold").FullPath
        else
            base.GetGoldTestDataPath(fileName)
