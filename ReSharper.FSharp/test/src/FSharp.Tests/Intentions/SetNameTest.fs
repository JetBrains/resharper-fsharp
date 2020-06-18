namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages("FSharp.Core")>]
type SetNameTest() =
    inherit FSharpContextActionExecuteTestBase<SetNameAction>()

    override x.ExtraPath = "setName"

    [<Test>] member x.``Let - Top 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Top 02 - StringBuilder``() = x.DoNamedTest()
    [<Test>] member x.``Let - Top 03 - Method``() = x.DoNamedTest()

    [<Test>] member x.``Match 01``() = x.DoNamedTest()
