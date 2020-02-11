namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages("FSharp.Core")>]
type InvertIfTest() =
    inherit FSharpContextActionExecuteTestBase<InvertIfAction>()

    override x.ExtraPath = "invertIf"

    [<Test>] member x.``Literal 01 - True``() = x.DoNamedTest()
    [<Test>] member x.``Literal 02 - False``() = x.DoNamedTest()

    [<Test>] member x.``Reference 01``() = x.DoNamedTest()
    [<Test>] member x.``Reference 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Reference 03 - Qualified``() = x.DoNamedTest()
    [<Test>] member x.``Reference 04 - Qualified with method``() = x.DoNamedTest()

    [<Test>] member x.``App - Not 01``() = x.DoNamedTest()

    [<Test>] member x.``App 01``() = x.DoNamedTest()
    [<Test>] member x.``App 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``App 03 - Inside other expr``() = x.DoNamedTest()

    [<Test>] member x.``Logic operators 01``() = x.DoNamedTest()
