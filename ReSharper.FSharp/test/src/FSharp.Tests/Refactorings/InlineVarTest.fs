namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Refactorings

open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Refactorings.Test.Common
open NUnit.Framework

[<FSharpTest>]
type InlineVarTest() =
    inherit InlineVarTestBase()

    override x.RelativeTestDataPath = "features/refactorings/inlineVar"

    override x.DoTest(lifetime, project) =
        use cookie = FSharpRegistryUtil.AllowExperimentalFeaturesCookie.Create()
        base.DoTest(lifetime, project)

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02``() = x.DoNamedTest()

    [<Test>] member x.``App 01``() = x.DoNamedTest()
    [<Test>] member x.``App 02 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Comment 01``() = x.DoNamedTest()

    [<Test>] member x.``Inline 01``() = x.DoNamedTest()

    [<Test>] member x.``Not available - Set 01``() = x.DoNamedTest()
