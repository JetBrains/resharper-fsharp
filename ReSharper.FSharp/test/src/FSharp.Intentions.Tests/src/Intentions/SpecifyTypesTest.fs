namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<AssertCorrectTreeStructure>]
type SpecifyTypesActionTest() =
    inherit FSharpContextActionExecuteTestBase<FunctionAnnotationAction>()

    override x.ExtraPath = "specifyTypes"

    [<Test>] member x.``Function - Parameters 01 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 02 - Wrong types``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 03 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 04 - Nested tuple``() = x.DoNamedTest()

    [<Test>] member x.``Function 01 - Unit to unit``() = x.DoNamedTest()
    [<Test>] member x.``Function 02 - Recursive``() = x.DoNamedTest()
    [<Test>] member x.``Function 03 - Generic types``() = x.DoNamedTest()
    [<Test>] member x.``Function 04 - Generalized``() = x.DoNamedTest()
    [<Test>] member x.``Function 05 - Specified return``() = x.DoNamedTest()

    [<Test>] member x.``Function - Local 01``() = x.DoNamedTest()

    [<Test>] member x.``Function - Parameters - Pattern 01 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 02 - Wild``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 03 - List``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 04 - As``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 05 - Param owner``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 06 - Tuple with as 01``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 07 - Tuple with as 02``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 08 - Tuple with as 03``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 09 - Nested tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 10 - Nested tuple 02``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 11 - Active pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 12 - Active pattern 02``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 13 - With attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 14 - With attribute 02``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 15 - With attribute 03``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 16 - With attribute 04``() = x.DoNamedTest()

    [<Test>] member x.``Function - Return - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Function - Return - Function 02``() = x.DoNamedTest()
    [<Test>] member x.``Function - Return - Function 03 - Specified function param``() = x.DoNamedTest()
    [<Test>] member x.``Function - Return - Function 04 - Function params``() = x.DoNamedTest()
    [<Test>] member x.``Function - Return - Function 05 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Function - Return 01``() = x.DoNamedTest()

    [<TestSetting(typeof<FSharpFormatSettingsKey>, "SpaceBeforeColon", "true")>]
    [<Test>] member x.``Function - Formatting - Add space``() = x.DoNamedTest()

    [<Test>] member x.``Value 01``() = x.DoNamedTest()
    [<Test>] member x.``Value 02 - Function``() = x.DoNamedTest()
    [<Test>] member x.``Value 03 - Function, tuple``() = x.DoNamedTest()

    [<Test>] member x.``Function - Caret on let binding``() = x.DoNamedTest()

    [<Test>] member x.``Function - Recursive - Function 01`` () = x.DoNamedTest()
    [<Test>] member x.``Function - Recursive - Function 02`` () = x.DoNamedTest()
    [<Test>] member x.``Function - Recursive - Function 03`` () = x.DoNamedTest()
    [<Test>] member x.``Function - Recursive - Function 04`` () = x.DoNamedTest()


type SpecifyTypesActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<FunctionAnnotationAction>()

    override x.ExtraPath = "specifyTypes"

    [<Test>] member x.``Let bindings - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Let bindings - Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Let bindings - Tuples 01``() = x.DoNamedTest()

    [<Test>] member x.``Class - member - 01``() = x.DoNamedTest()
    [<Test>] member x.``LetBang - 01`` () = x.DoNamedTest()
    [<Test>] member x.``UseBang - 01`` () = x.DoNamedTest()
    [<Test>] member x.``AndBang - 01`` () = x.DoNamedTest()
