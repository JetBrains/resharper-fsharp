namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type AddEmptyAttributeToParameterActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<AddEmptyAttributeToParameterAction>()
    override x.ExtraPath = "addEmptyAttribute"
    [<Test>] member x.``Parameter - Availability``() = x.DoNamedTest()

type AddEmptyAttributeToParameterActionTest() =
    inherit FSharpContextActionExecuteTestBase<AddEmptyAttributeToParameterAction>()

    override x.ExtraPath = "addEmptyAttribute"

    [<Test>] member x.``Functions - Parameters 01 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Functions - Parameters 02 - NoParens``() = x.DoNamedTest()
    [<Test>] member x.``Functions - Parameters 03 - ExistingAttribute``() = x.DoNamedTest()
    [<Test>] member x.``Functions - Parameters 04 - ExistingAttributes``() = x.DoNamedTest()
    [<Test>] member x.``Functions - Parameters 05 - Typed - ExistingAttribute``() = x.DoNamedTest()

    [<Test>] member x.``Methods - Parameters 01 - NoParens``() = x.DoNamedTest()
    [<Test>] member x.``Methods - Parameters 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Methods - Parameters 03 - ExistingAttribute``() = x.DoNamedTest()

type AddEmptyAttributeToMemberActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<AddEmptyAttributeToMemberAction>()
    override x.ExtraPath = "addEmptyAttribute"
    [<Test>] member x.``Member - Availability``() = x.DoNamedTest()

type AddEmptyAttributeToMemberActionTest() =
    inherit FSharpContextActionExecuteTestBase<AddEmptyAttributeToMemberAction>()

    override x.ExtraPath = "addEmptyAttribute"

    [<Test>] member x.``Methods 01 - NoAttribute``() = x.DoNamedTest()
    [<Test>] member x.``Methods 02 - ExistingAttribute``() = x.DoNamedTest()

    [<Test>] member x.``Properties 01 - NoAttribute``() = x.DoNamedTest()
    [<Test>] member x.``Properties 02 - ExistingAttribute``() = x.DoNamedTest()

type AddEmptyAttributeToTypeActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<AddEmptyAttributeToTypeAction>()
    override x.ExtraPath = "addEmptyAttribute"
    [<Test>] member x.``Type - Availability``() = x.DoNamedTest()

type AddEmptyAttributeToTypeActionTest() =
    inherit FSharpContextActionExecuteTestBase<AddEmptyAttributeToTypeAction>()

    override x.ExtraPath = "addEmptyAttribute"

    [<Test>] member x.``Types 01 - NoAttribute``() = x.DoNamedTest()
    [<Test>] member x.``Types 02 - ExistingAttribute``() = x.DoNamedTest()

type AddEmptyAttributeToModuleActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<AddEmptyAttributeToModuleAction>()
    override x.ExtraPath = "addEmptyAttribute"
    [<Test>] member x.``Module - Availability``() = x.DoNamedTest()

type AddEmptyAttributeToModuleActionTest() =
    inherit FSharpContextActionExecuteTestBase<AddEmptyAttributeToModuleAction>()

    override x.ExtraPath = "addEmptyAttribute"

    [<Test>] member x.``Modules 01 - NoAttribute``() = x.DoNamedTest()
    [<Test>] member x.``Modules 02 - ExistingAttribute``() = x.DoNamedTest()

type AddEmptyAttributeToBindingActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<AddEmptyAttributeToBindingAction>()
    override x.ExtraPath = "addEmptyAttribute"
    [<Test>] member x.``Binding - Availability``() = x.DoNamedTest()

type AddEmptyAttributeToBindingActionTest() =
    inherit FSharpContextActionExecuteTestBase<AddEmptyAttributeToBindingAction>()

    override x.ExtraPath = "addEmptyAttribute"

    [<Test>] member x.``Bindings 01 - NoAttribute``() = x.DoNamedTest()
    [<Test>] member x.``Bindings 02 - ExistingAttribute``() = x.DoNamedTest()