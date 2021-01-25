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

    [<Test>] member x.``App 01``() = x.DoNamedTest()
    [<Test>] member x.``App 02 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Comment 01``() = x.DoNamedTest()
    [<Test>] member x.``Comment 02 - Empty line``() = x.DoNamedTest()
    [<Test>] member x.``Comment 03 - Empty lines``() = x.DoNamedTest()

    [<Test>] member x.``In keyword 01``() = x.DoNamedTest()
    [<Test>] member x.``In keyword 02 - Comment``() = x.DoNamedTest()

    [<Test>] member x.``Inline 01``() = x.DoNamedTest()
    [<Test; Explicit("Parsed wrongly: dotnet/fsharp#7741")>] member x.``Inline 02 - Multiline body``() = x.DoNamedTest()

    [<Test>] member x.``Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 02``() = x.DoNamedTest()

    [<Test>] member x.``Space 01``() = x.DoNamedTest()

    [<Test>] member x.``Not available - AddressOf 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Set 01``() = x.DoNamedTest()
