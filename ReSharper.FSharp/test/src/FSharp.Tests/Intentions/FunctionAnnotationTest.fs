namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages("FSharp.Core")>]
type FunctionAnnotationActionTest() =
    inherit FSharpContextActionExecuteTestBase<FunctionAnnotationAction>()

    override x.ExtraPath = "functionAnnotation"
    
    [<Test>] member x.``Let - No existing annotations``() = x.DoNamedTest()
    [<Test>] member x.``Let nested - Mixed existing annotations``() = x.DoNamedTest()
    [<Test>] member x.``Let - multiline mixed existing annotations``() = x.DoNamedTest()
    [<Test>] member x.``Let - existing return type annotation``() = x.DoNamedTest()
    [<Test>] member x.``Let - existing incorrect return type annotation``() = x.DoNamedTest()
    [<Test>] member x.``Let - existing incorrect parameters annotation``() = x.DoNamedTest()
    [<Test>] member x.``Let - unable to determine all parameter or return types``() = x.DoNamedTest()

[<FSharpTest; TestPackages("FSharp.Core")>]
type FunctionAnnotationAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<FunctionAnnotationAction>()

    override x.ExtraPath = "functionAnnotation"

    [<Test>] member x.``Module - Name 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Name 02 - Attributes``() = x.DoNamedTest()

    [<Test>] member x.``Module - Keyword 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Keyword 02 - Attributes``() = x.DoNamedTest()

    [<Test>] member x.``Expression - Name 01``() = x.DoNamedTest()
    [<Test>] member x.``Expression - Keyword 01``() = x.DoNamedTest()

    [<Test>] member x.``Not available``() = x.DoNamedTest()