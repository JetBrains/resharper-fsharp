namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

type RenameFileToMatchTypeNameActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<RenameFileToMatchTypeNameAction>()

    override x.ExtraPath = "renameFileToMatchTypeName"

    [<Test>] member x.``Module - Nested 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested 02 - Multiple``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested 03 - Associated``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested 04 - Associated``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested 04``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top level 01``() = x.DoNamedTest()

    [<Test>] member x.``Namespace 01``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 02 - Multiple types``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 03 - Multiple namespaces``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 04 - Multiple namespaces``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 05 - Type extension``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 06 - Type extension``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 07 - Exception``() = x.DoNamedTest()
