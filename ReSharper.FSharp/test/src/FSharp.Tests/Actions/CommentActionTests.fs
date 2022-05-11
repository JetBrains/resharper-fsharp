namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Actions

open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
[<TestSettingsKey(typeof<FSharpFormatSettingsKey>)>]
[<TestSettings("{PlaceCommentsAtFirstColumn:All, StickComment:All}")>]
type FSharpCommentLineTests() =
    inherit ExecuteActionIteratableTestBase()

    override x.RelativeTestDataPath = "actions/comment"
    override x.ActionId = "LineComment"

    [<Test>] member x.testLine01() = x.DoNamedTest()
    [<Test>] member x.testLine02() = x.DoNamedTest()
    [<Test>] member x.testLine03() = x.DoNamedTest()
    [<Test>] member x.testLine04() = x.DoNamedTest()
    [<Test>] member x.testLine05() = x.DoNamedTest()
    [<Test>] member x.testLine06() = x.DoNamedTest()
    [<Test>] member x.testLine07() = x.DoNamedTest()
