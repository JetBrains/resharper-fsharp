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
type TypeHintStageTest() =
    inherit FSharpHighlightingTestBase()

    let [<Literal>] PatternsCommonTestFile = "_TestSource.fs"
    override x.RelativeTestDataPath = "features/daemon/typeHints"

    [<Test>] member x.``Signatures - Show all 01``() = x.DoPatternsCommonTest()

    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForLocalBindings", PushToHintMode.Never)>]
    [<Test>] member x.``Signatures - Top level 01``() = x.DoPatternsCommonTest()

    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForTopLevelMembers", PushToHintMode.Never)>]
    [<Test>] member x.``Signatures - Locals 01``() = x.DoPatternsCommonTest()

    [<TestSetting(typeof<GeneralInlayHintsOptions>, "EnableInlayHints", false)>]
    [<Test>] member x.``Disabled 01``() = x.DoPatternsCommonTest()

    [<TestSetting(typeof<GeneralInlayHintsOptions>, "DefaultMode", PushToHintMode.Never)>]
    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForTopLevelMembers", PushToHintMode.Default)>]
    [<TestSetting(typeof<FSharpTypeHintOptions>, "ShowTypeHintsForLocalBindings", PushToHintMode.Default)>]
    [<Test>] member x.``Disabled 02 - By mode``() = x.DoPatternsCommonTest()

    [<Test>] member x.``Signatures - Unfinished declarations 01``() = x.DoNamedTest()

    member x.DoPatternsCommonTest() = x.DoTestSolution(PatternsCommonTestFile)

    override x.GetGoldTestDataPath(fileName) =
        if fileName = PatternsCommonTestFile then
            x.GetTestDataFilePath2(x.TestMethodName + ".fs.gold").FullPath
        else
            base.GetGoldTestDataPath(fileName)
