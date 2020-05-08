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
    [<Test>] member x.``Simple 02``() = x.DoNamedTest()
    [<Test>] member x.``Simple 03``() = x.DoNamedTest()

    [<Test>] member x.``Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Let 02 - Function``() = x.DoNamedTest()
    [<Test>] member x.``Let 03 - Inside other``() = x.DoNamedTest()

    [<Test>] member x.``Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Match 02 - Multiline``() = x.DoNamedTest()

    [<Test>] member x.``LetExpr 01``() = x.DoNamedTest()

    [<Test>] member x.``Seq 01``() = x.DoNamedTest()
    [<Test>] member x.``Seq 02``() = x.DoNamedTest()
    [<Test>] member x.``Seq 03 - Last``() = x.DoNamedTest()

    [<Test>] member x.``LetDecl 01``() = x.DoNamedTest()
    [<Test>] member x.``LetDecl 02 - Indent``() = x.DoNamedTest()

    [<Test>] member x.``Do decl - Implicit 01``() = x.DoNamedTest()
    [<Test>] member x.``Do decl - Implicit 02 - App``() = x.DoNamedTest()

    [<Test>] member x.``Shift - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Shift - Decl 02``() = x.DoNamedTest()

    [<Test>] member x.``Shift - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Shift - Expr 02``() = x.DoNamedTest()

    [<Test>] member x.``Not allowed - Attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - Named arg 01``() = x.DoNamedTest()
