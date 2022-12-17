namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ContextActions
open NUnit.Framework

type ExpandToLambdaTest() =
    inherit FSharpContextActionExecuteTestBase<ExpandToLambdaAction>()

    override x.ExtraPath = "expandToLambda"

    [<Test>] member x.``Method 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Method 02 - Parameter groups``() = x.DoNamedTest()
    [<Test>] member x.``Method 03 - Extension``() = x.DoNamedTest()
    [<Test>] member x.``Method 04 - Unit``() = x.DoNamedTest()

    [<Test>] member x.``Constructor 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Constructor 02 - Union case``() = x.DoNamedTest()

    [<Test>] member x.``Binding 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Binding 02 - Parameter groups``() = x.DoNamedTest()
    [<Test>] member x.``Binding 03 - Fun``() = x.DoNamedTest()
    [<Test>] member x.``Binding 04 - Local``() = x.DoNamedTest()


type ExpandToLambdaAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ExpandToLambdaAction>()

    override x.ExtraPath = "expandToLambda"

    [<Test; ActionNotAvailable>] member x.``Application 01 - Not available``() = x.DoNamedTest()
    [<Test; ActionNotAvailable>] member x.``Bindings 01``() = x.DoNamedTest()
