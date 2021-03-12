namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Refactorings

open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Refactorings.Test.Common
open NUnit.Framework

[<FSharpTest>]
type InlineVarTest() =
    inherit InlineVarTestBase()

    override x.RelativeTestDataPath = "features/refactorings/inlineVar"

    override x.DoTest(lifetime, project) =
        use cookie = FSharpExperimentalFeatures.EnableInlineVarRefactoringCookie.Create()
        base.DoTest(lifetime, project)

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02``() = x.DoNamedTest()

    [<Test>] member x.``App - Method arg - App - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Method arg - App - Tuple 02``() = x.DoNamedTest()
    [<Test>] member x.``App - Method arg - App 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Method arg - App 02``() = x.DoNamedTest()

    [<Test>] member x.``App 01``() = x.DoNamedTest()
    [<Test>] member x.``App 02 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Comment 01``() = x.DoNamedTest()
    [<Test>] member x.``Comment 02 - Empty line``() = x.DoNamedTest()
    [<Test>] member x.``Comment 03 - Empty lines``() = x.DoNamedTest()

//    [<Test>] member x.``If - Then - Indent 01``() = x.DoNamedTest()
//    [<Test>] member x.``If - Then - Indent 02``() = x.DoNamedTest()
//    [<Test>] member x.``If - Then - Indent 03``() = x.DoNamedTest()
//    [<Test>] member x.``If - Then - Indent 04``() = x.DoNamedTest()
//    [<Test>] member x.``If - Then - Single line 01``() = x.DoNamedTest()
//    [<Test>] member x.``If - Then - Single line 02 - No else``() = x.DoNamedTest()

    [<Test>] member x.``In keyword 01``() = x.DoNamedTest()
    [<Test>] member x.``In keyword 02 - Comment``() = x.DoNamedTest()

    [<Test>] member x.``Inline 01``() = x.DoNamedTest()
    [<Test; Explicit("Parsed wrongly: dotnet/fsharp#7741")>] member x.``Inline 02 - Multiline body``() = x.DoNamedTest()

//    [<Test>] member x.``Match - Expr - App - Binary 01``() = x.DoNamedTest()
//    [<Test>] member x.``Match - Expr - App 01``() = x.DoNamedTest()
//    [<Test>] member x.``Match - Expr - App 02``() = x.DoNamedTest()
//    [<Test>] member x.``Match - Expr - App 03``() = x.DoNamedTest()
//    [<Test>] member x.``Match - Expr - If 01``() = x.DoNamedTest()
//    [<Test>] member x.``Match - Expr - Literal 01``() = x.DoNamedTest()
//
//    [<Test>] member x.``Match - Expr - Match - Multiline 01``() = x.DoNamedTest()
//    [<Test>] member x.``Match - Expr - Match - Multiline 02``() = x.DoNamedTest()
//    [<Test>] member x.``Match - Expr - Match - Multiline 03``() = x.DoNamedTest()
//    [<Test>] member x.``Match - Expr - Match 01``() = x.DoNamedTest()

    [<Test>] member x.``Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 02``() = x.DoNamedTest()

    [<Test>] member x.``Qualifier - Ident 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualifier - Literal 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualifier - Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualifier - Method 02``() = x.DoNamedTest()

    [<Test>] member x.``Space 01``() = x.DoNamedTest()

    [<Test>] member x.``Not available - AddressOf 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Set 01``() = x.DoNamedTest()
