namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Actions

open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<Category("CommentAction")>]
type FSharpCommentLineTests() =
    inherit ExecuteActionTestBase()

    override x.RelativeTestDataPath = @"actions\comment"
    override x.ActionId = "LineComment"

    [<Test>] member x.testLine01() = x.DoTestFiles("testLine01.fs")
    [<Test>] member x.testLine02() = x.DoTestFiles("testLine02.fs")
    [<Test>] member x.testLine03() = x.DoTestFiles("testLine03.fs")
    [<Test>] member x.testLine04() = x.DoTestFiles("testLine04.fs")
    [<Test>] member x.testLine05() = x.DoTestFiles("testLine05.fs")
