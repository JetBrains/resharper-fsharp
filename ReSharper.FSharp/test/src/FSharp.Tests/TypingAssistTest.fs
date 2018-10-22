namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.TypingAssist

open JetBrains.ReSharper.FeaturesTestFramework.TypingAssist
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestFileExtension(FSharpProjectFileType.FsExtension)>]
[<TestSettingsKey(typeof<FSharpFormatSettingsKey>)>]
type FSharpTypingAssistTest() =
    inherit TypingAssistTestBase()

    override x.RelativeTestDataPath = "features/service/typingAssist"

    [<Test>] member x.``Enter 01 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Enter 02 - Dumb indent``() = x.DoNamedTest()
