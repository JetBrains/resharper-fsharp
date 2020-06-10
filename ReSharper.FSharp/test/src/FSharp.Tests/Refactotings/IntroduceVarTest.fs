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
    [<Test>] member x.``Let 04 - After other``() = x.DoNamedTest()

    [<Test>] member x.``Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Match 02 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Match 03 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Match 04 - Multiline``() = x.DoNamedTest()

    [<Test>] member x.``LetExpr 01``() = x.DoNamedTest()

    [<Test>] member x.``Seq 01``() = x.DoNamedTest()
    [<Test>] member x.``Seq 02``() = x.DoNamedTest()
    [<Test>] member x.``Seq 03 - Last``() = x.DoNamedTest()
    [<Test>] member x.``Seq 04``() = x.DoNamedTest()

    [<Test>] member x.``LetDecl - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``LetDecl - Function 02``() = x.DoNamedTest()
    [<Test>] member x.``LetDecl 01``() = x.DoNamedTest()
    [<Test>] member x.``LetDecl 02 - Indent``() = x.DoNamedTest()

    [<Test>] member x.``Do decl - Implicit 01``() = x.DoNamedTest()
    [<Test>] member x.``Do decl - Implicit 02 - App``() = x.DoNamedTest()

    [<Test>] member x.``Member - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Auto property 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Binary app 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Binary app - Same indent 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Binary app - Same indent 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Binary app - Same indent 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Binary app - Same indent 04``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Binary app - Same indent 05``() = x.DoNamedTest()

    [<Test>] member x.``Record field binding - Anon 01``() = x.DoNamedTest()
    [<Test>] member x.``Record field binding - Anon 02 - In app``() = x.DoNamedTest()
    [<Test>] member x.``Record field binding 01``() = x.DoNamedTest()
    [<Test>] member x.``Record field binding 02 - In app``() = x.DoNamedTest()

    [<Test>] member x.``Shift - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Shift - Decl 02``() = x.DoNamedTest()

    [<Test>] member x.``Shift - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Shift - Expr 02``() = x.DoNamedTest()

    [<Test>] member x.``Single line - If 01``() = x.DoNamedTest()
    [<Test>] member x.``Single line - Lambda 01``() = x.DoNamedTest()
    [<Test>] member x.``Single line - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Single line - Match 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Single line - When 01``() = x.DoNamedTest()

    [<Test>] member x.``Types 01 - Binary app``() = x.DoNamedTest()
    [<Test>] member x.``Types 02 - Method return``() = x.DoNamedTest()
    [<Test>] member x.``Types 03 - Type check``() = x.DoNamedTest()

    [<Test>] member x.``Not allowed - Attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - CompExpr 01``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - CompExpr 02 - Yield``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - RangeSequenceExpr 01``() = x.DoNamedTest()

    [<Test>] member x.``Not allowed - Named arg 01``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - Named arg 02 - Unit``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - Named arg 03 - Union case field``() = x.DoNamedTest()
