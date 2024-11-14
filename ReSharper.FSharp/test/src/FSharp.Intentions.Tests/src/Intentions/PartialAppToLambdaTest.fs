namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions
open NUnit.Framework

[<AssertCorrectTreeStructure>]
type PartialAppToLambdaTest() =
    inherit FSharpContextActionExecuteTestBase<ConvertPartiallyAppliedFunctionToLambdaAction>()

    override this.ExtraPath = "partialToLambda"

    [<Test>] member x.``Args 01``() = x.DoNamedTest()

    [<Test>] member x.``Op 01``() = x.DoNamedTest()
    [<Test>] member x.``Op 02``() = x.DoNamedTest()
    [<Test>] member x.``Op 03``() = x.DoNamedTest()

    [<Test>] member x.``Ref 01``() = x.DoNamedTest()
