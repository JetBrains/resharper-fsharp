namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Feature.Services.Tests.FeatureServices.SelectEmbracingConstruct
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FSharpExtendSelectionTest() =
    inherit SelectEmbracingConstructTestBase()

    override x.RelativeTestDataPath = "features/service/extendSelection"

    [<Test>] member x.``Brack - Array - Right 01``() = x.DoNamedTest()
    [<Test>] member x.``Brack - Array - Right 02``() = x.DoNamedTest()
    [<Test>] member x.``Brack - List - Right 01``() = x.DoNamedTest()
    [<Test>] member x.``Brack - List - Right 02``() = x.DoNamedTest()

    [<Test>] member x.``Module qualifier 01 - Name``() = x.DoNamedTest()
    [<Test>] member x.``Module qualifier 02 - Qualifier``() = x.DoNamedTest()
    [<Test>] member x.``Module qualifier 03 - Multiple qualifiers``() = x.DoNamedTest()

    [<Test>] member x.``Type extension qualifier 01 - Name``() = x.DoNamedTest()
    [<Test>] member x.``Type extension qualifier 02 - Qualifier``() = x.DoNamedTest()

    [<Test>] member x.``Match clause - When 01 - Pat``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - When 02 - When``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - When 03 - Expr``() = x.DoNamedTest()

    [<Test>] member x.``Let - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Binding 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Keyword 01``() = x.DoNamedTest()

    [<Test>] member x.``Let - Rec 01 - Let``() = x.DoNamedTest()
    [<Test>] member x.``Let - Rec 02 - First``() = x.DoNamedTest()
    [<Test>] member x.``Let - Rec 03 - Other``() = x.DoNamedTest()
    [<Test>] member x.``Let - Rec 04 - Rec``() = x.DoNamedTest()
    [<Test>] member x.``Let - Rec 05 - And``() = x.DoNamedTest()

    [<Test>] member x.``Let - Top level 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Top level 02 - Rec, other``() = x.DoNamedTest()
    [<Test>] member x.``Let - Top level 03 - Rec, attrs``() = x.DoNamedTest()

    [<Test>] member x.``Infix expr - Argument``() = x.DoNamedTest()
    [<Test>] member x.``Infix expr - Operator``() = x.DoNamedTest()
    [<Test>] member x.``Infix expr - Nested - Argument 01``() = x.DoNamedTest()
    [<Test>] member x.``Infix expr - Nested - Argument 02``() = x.DoNamedTest()
    [<Test>] member x.``Infix expr - Nested - Operator 01``() = x.DoNamedTest()
    [<Test>] member x.``Infix expr - Nested - Operator 02``() = x.DoNamedTest()

    [<Test>] member x.``Separator - Comma 01 - Literal``() = x.DoNamedTest()
    [<Test>] member x.``Separator - Comma 02 - Ident``() = x.DoNamedTest()
    [<Test>] member x.``Separator - Comma 03 - Underscore``() = x.DoNamedTest()
    [<Test>] member x.``Separator - Semi 01``() = x.DoNamedTest()
    [<Test>] member x.``Separator - Semi 02``() = x.DoNamedTest()

    [<Test>] member x.``Unit expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Unit expr 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Unit expr 03 - Space inside``() = x.DoNamedTest()

    [<Test>] member x.``Type inheritance 01 - Type name``() = x.DoNamedTest()
    [<Test>] member x.``Type inheritance 02 - Ctor arg``() = x.DoNamedTest()

    [<Test>] member x.``Target attribute 01 - Type name``() = x.DoNamedTest()
    [<Test>] member x.``Target attribute 02 - Arg expr``() = x.DoNamedTest()

    [<Test>] member x.``Dot 01``() = x.DoNamedTest()
    [<Test>] member x.``Dot 02``() = x.DoNamedTest()

    [<Test>] member x.``Greater 01``() = x.DoNamedTest()
    [<Test>] member x.``Greater 02``() = x.DoNamedTest()
    [<Test>] member x.``Greater 03``() = x.DoNamedTest()
    [<Test>] member x.``Less 01``() = x.DoNamedTest()
    [<Test>] member x.``Plus 01``() = x.DoNamedTest()

    [<Test>] member x.``Strings - Regular - Empty``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Regular - Multiple words``() = x.DoNamedTest()

    [<Test>] member x.``Strings - Interpolated - Empty``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Interpolated - With inserts 01``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Interpolated - With inserts 02``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Interpolated - Without expressions``() = x.DoNamedTest()

