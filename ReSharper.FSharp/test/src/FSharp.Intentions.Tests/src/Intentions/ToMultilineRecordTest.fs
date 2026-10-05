namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type ToMultilineRecordExecuteTest() =
    inherit FSharpContextActionExecuteTestBase<ToMultilineRecord>()

    override x.ExtraPath = "toMultilineRecord"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02 - No space``() = x.DoNamedTest()
    [<Test>] member x.``Simple 03 - Comment``() = x.DoNamedTest()
    [<Test>] member x.``Simple 04 - Spread``() = x.DoNamedTest()
    [<Test>] member x.``Simple 05 - Spread only``() = x.DoNamedTest()
    [<Test>] member x.``Simple 06 - Spread last``() = x.DoNamedTest()

type ToMultilineRecordAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ToMultilineRecord>()

    override x.ExtraPath = "toMultilineRecord"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02 - Copy expr``() = x.DoNamedTest()
