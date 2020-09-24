namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

type SimplifyListConsPatTest() =
    inherit FSharpQuickFixTestBase<SimplifyListConsPatFix>()

    override x.RelativeTestDataPath = "features/quickFixes/simplifyListConsPat"

    [<Test>] member x.``Test - Comment 01``() = x.DoNamedTest()
    [<Test>] member x.``Test - Comment 02 - Add space``() = x.DoNamedTest()

    [<TestSetting(typeof<FSharpFormatSettingsKey>, "SpaceAroundDelimiter", "false")>]
    [<Test>] member x.``Test - Comment 03 - Remove space``() = x.DoNamedTest()

    [<TestSetting(typeof<FSharpFormatSettingsKey>, "SpaceAroundDelimiter", "false")>]
    [<Test>] member x.``Test - No space 01``() = x.DoNamedTest()

    [<TestSetting(typeof<FSharpFormatSettingsKey>, "SpaceAroundDelimiter", "false")>]
    [<Test>] member x.``Test - No space 02``() = x.DoNamedTest()

    [<Test>] member x.``Test 01``() = x.DoNamedTest()
    [<Test>] member x.``Test 02 - Space``() = x.DoNamedTest()
