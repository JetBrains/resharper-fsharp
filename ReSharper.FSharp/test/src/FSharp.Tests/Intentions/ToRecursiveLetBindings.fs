namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type ToRecursiveLetBindingsExecuteTest() =
    inherit FSharpContextActionExecuteTestBase<ToRecursiveLetBindings>()

    override x.ExtraPath = "toRecursiveLetBindings"

    [<Test>] member x.``Module - Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Space 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Next line 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Next line 02``() = x.DoNamedTest()

    [<Test>] member x.``Expression - Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Expression - Next line 01``() = x.DoNamedTest()

type ToRecursiveLetBindingsAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ToRecursiveLetBindings>()

    override x.ExtraPath = "toRecursiveLetBindings"

    [<Test>] member x.``Module - Name 01``() = x.DoNamedTest()
    [<Test; Explicit("Fix getting keyword")>] member x.``Name 02 - Attributes``() = x.DoNamedTest()

    [<Test>] member x.``Module - Keyword 01``() = x.DoNamedTest()
    [<Test; Explicit("Fix getting keyword")>] member x.``Keyword 02 - Attributes``() = x.DoNamedTest()

    [<Test>] member x.``Expression - Name 01``() = x.DoNamedTest()
    [<Test>] member x.``Expression - Keyword 01``() = x.DoNamedTest()

    [<Test>] member x.``Not available``() = x.DoNamedTest()
