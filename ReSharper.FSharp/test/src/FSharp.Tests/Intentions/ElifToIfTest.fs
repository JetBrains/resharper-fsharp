namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ElifToIfTest() =
    inherit FSharpContextActionExecuteTestBase<ElifToIfAction>()

    override x.ExtraPath = "elifToIf"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02 - No spaces``() = x.DoNamedTest()
