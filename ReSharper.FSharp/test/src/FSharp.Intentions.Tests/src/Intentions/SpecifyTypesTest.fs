namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

type FunctionAnnotationActionTest() =
    inherit FSharpContextActionExecuteTestBase<FunctionAnnotationAction>()

    override x.ExtraPath = "specifyTypes/functions"

    [<Test>] member x.``Function - Parameters 01 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 02 - Wrong types``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 03 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 04 - Nested tuple``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 05 - Nested parens``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 06 - Partially typed``() = x.DoNamedTest()

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

// TODO: more tests
type FunctionAnnotationActionActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<FunctionAnnotationAction>()

    override x.ExtraPath = "specifyTypes/functions"

    [<Test>] member x.``Function - Let bindings - Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Function - Let bindings - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Function - Class - member - 01``() = x.DoNamedTest()
    [<Test>] member x.``Value - Let bindings - Expr 01``() = x.DoNamedTest()

type ValueAnnotationActionTest() =
    inherit FSharpContextActionExecuteTestBase<ValueAnnotationAction>()

    override x.ExtraPath = "specifyTypes/values"

    [<Test>] member x.``Value 01``() = x.DoNamedTest()
    [<Test>] member x.``Value 02 - Function``() = x.DoNamedTest()
    [<Test>] member x.``Value 03 - Function, tuple``() = x.DoNamedTest()
    [<Test>] member x.``Value 04``() = x.DoNamedTest()
    [<Test>] member x.``Value 05 - Function Parameter With Type Usage``() = x.DoNamedTest()
    [<Test>] member x.``Value 06 - Partial Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Value 07 - Attribute On Return Type``() = x.DoNamedTest()

    [<Test>] member x.``Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 02 - Child tuple``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 03 - Parent tuple - 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 04 - Parent tuple - 02``() = x.DoNamedTest()

    [<Test>] member x.``Value - Async - 01``() = x.DoNamedTest()
    [<Test>] member x.``Value - Async - 02``() = x.DoNamedTest()
    [<Test>] member x.``Value - Async - 03``() = x.DoNamedTest()
    [<Test>] member x.``Value - Async - 04``() = x.DoNamedTest()

    [<Test>] member x.``Value - Interface Method Parameter - 01``() = x.DoNamedTest()
    [<Test>] member x.``Value - Method Parameter - 01``() = x.DoNamedTest()
    [<Test>] member x.``Value - Method Parameter - 02``() = x.DoNamedTest()

    [<Test>] member x.``Value - Static Method Parameter - 01``() = x.DoNamedTest()
    [<Test>] member x.``Value - Static Method Parameter - 02``() = x.DoNamedTest()

type ValueAnnotationActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ValueAnnotationAction>()

    override x.ExtraPath = "specifyTypes/values"

    [<Test>] member x.``Value - Let bindings - Async``() = x.DoNamedTest()
    [<Test>] member x.``Value - Let bindings - Module``() = x.DoNamedTest()
    [<Test>] member x.``Value - Let bindings - Expr``() = x.DoNamedTest()
    [<Test>] member x.``Value - Let bindings - Type Usage``() = x.DoNamedTest()
    [<Test>] member x.``Value - Interface Method Parameter``() = x.DoNamedTest()

// TODO: Tests
type MemberAnnotationActionTest() =
    inherit FSharpContextActionExecuteTestBase<MemberAnnotationAction>()

    override x.ExtraPath = "specifyTypes/members"

    [<Test>] member x.``Member - Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Method 02``() = x.DoNamedTest()
    [<Test>] member x.``Member - Method 03``() = x.DoNamedTest()
    [<Test>] member x.``Member - Method 04``() = x.DoNamedTest()

    [<Test>] member x.``Member - Static Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Static Method 02``() = x.DoNamedTest()

    [<Test>] member x.``Member - Interface Method 01``() = x.DoNamedTest()

    [<Test>] member x.``Member - Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Property 02``() = x.DoNamedTest()
    [<Test>] member x.``Member - Property 03``() = x.DoNamedTest()

    // TODO: fix these
//    [<Test>] member x.``Member - Val 01``() = x.DoNamedTest()
//    [<Test>] member x.``Member - Val 02``() = x.DoNamedTest()

type MemberAnnotationActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<MemberAnnotationAction>()

    override x.ExtraPath = "specifyTypes/members"

    [<Test>] member x.``Class - member - 01``() = x.DoNamedTest()
    [<Test>] member x.``Interface - member - 01``() = x.DoNamedTest()