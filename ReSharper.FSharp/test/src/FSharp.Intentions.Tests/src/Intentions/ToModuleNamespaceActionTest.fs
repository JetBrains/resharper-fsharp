namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type ToModuleNamespaceActionExecuteTest() =
    inherit FSharpContextActionExecuteTestBase<ToModuleNamespaceDeclarationAction>()

    override x.ExtraPath = "toModuleNamespace"

    [<Test>] member x.``Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Module 02``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 01``() = x.DoNamedTest()


type ToModuleNamespaceActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ToModuleNamespaceDeclarationAction>()

    override x.ExtraPath = "toModuleNamespace"

    [<Test>] member x.``Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Module 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 01``() = x.DoNamedTest()
