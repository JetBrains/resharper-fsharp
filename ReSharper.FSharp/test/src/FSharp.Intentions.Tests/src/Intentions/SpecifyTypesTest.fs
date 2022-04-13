namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

// TODO: Tests
type FunctionAnnotationActionTest() =
    inherit FSharpContextActionExecuteTestBase<FunctionAnnotationAction>()

    override x.ExtraPath = "specifyTypes/bindings"

    [<Test>] member x.``Function - Parameters 01 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 02 - Wrong types``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 03 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 04 - Nested tuple``() = x.DoNamedTest()

    [<Test>] member x.``Function 01 - Unit to unit``() = x.DoNamedTest()
    [<Test>] member x.``Function 02 - Recursive``() = x.DoNamedTest()
    [<Test>] member x.``Function 03 - Generic types``() = x.DoNamedTest()
    [<Test>] member x.``Function 04 - Generalized``() = x.DoNamedTest()
    [<Test>] member x.``Function 05 - Specified return``() = x.DoNamedTest()
    [<Test>] member x.``Function 06 - No param``() = x.DoNamedTest()

    [<Test>] member x.``Function - Local 01``() = x.DoNamedTest()

    [<Test>] member x.``Function - Parameters - Pattern 01 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 02 - Wild``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 03 - List``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 04 - As``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 05 - Param owner``() = x.DoNamedTest()

     [<Test>] member x.``Function - Let and 01``() = x.DoNamedTest()
     [<Test>] member x.``Function - Let and 02``() = x.DoNamedTest()

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

// TODO: more tests
type FunctionAnnotationActionActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<FunctionAnnotationAction>()

    override x.ExtraPath = "specifyTypes/bindings"

    [<Test>] member x.``Function - Let bindings - Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Function - Let bindings - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Function - Class - member - 01``() = x.DoNamedTest()

type ValueAnnotationActionTest() =
    inherit FSharpContextActionExecuteTestBase<ValueAnnotationAction>()

    override x.ExtraPath = "specifyTypes/bindings"

    //[<Test>] member x.``Function - Parameters 01 - Parens``() = x.DoNamedTest()

// TODO: Tests
type ValueAnnotationActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ValueAnnotationAction>()

    override x.ExtraPath = "specifyTypes/values"

    //[<Test>] member x.``Value - Let bindings - Expr 01``() = x.DoNamedTest()

// TODO: Tests
type MemberAnnotationActionTest() =
    inherit FSharpContextActionExecuteTestBase<MemberAnnotationAction>()

    override x.ExtraPath = "specifyTypes/bindings"

    //[<Test>] member x.``Function - Parameters 01 - Parens``() = x.DoNamedTest()

// TODO: Tests
type MemberAnnotationActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<MemberAnnotationAction>()

    override x.ExtraPath = "specifyTypes/members"

    //[<Test>] member x.``Value - Let bindings - Expr 01``() = x.DoNamedTest()

// TODO: Tests
type TupleAnnotationActionTest() =
    inherit FSharpContextActionExecuteTestBase<TupleAnnotationAction>()

    override x.ExtraPath = "specifyTypes/bindings"

    //[<Test>] member x.``Function - Parameters 01 - Parens``() = x.DoNamedTest()

// TODO: Tests
type TupleAnnotationActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<TupleAnnotationAction>()

    override x.ExtraPath = "specifyTypes/tuples"

    //[<Test>] member x.``Value - Let bindings - Expr 01``() = x.DoNamedTest()