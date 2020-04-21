namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Refactorings

open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Refactorings.Test.Common
open NUnit.Framework

[<FSharpTest>]
type IntroduceVarTest() =
    inherit IntroduceVariableTestBase()

    override x.RelativeTestDataPath = "features/refactorings/introduceVar"

    override x.DoTest(lifetime, project) =
        use cookie = FSharpRegistryUtil.AllowExperimentalFeaturesCookie.Create()
        base.DoTest(lifetime, project)

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()

    [<Test>] member x.``Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Let 02 - Function``() = x.DoNamedTest()
    [<Test>] member x.``Let 03 - Inside other``() = x.DoNamedTest()
