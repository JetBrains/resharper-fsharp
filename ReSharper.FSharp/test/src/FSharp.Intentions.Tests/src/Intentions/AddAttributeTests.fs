namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type AddAttributeToParameterActionTest() =
    inherit FSharpContextActionExecuteTestBase<AddAttributeToParameterAction>()

    override x.ExtraPath = "addAttribute"

    [<Test>] member x.``Functions - Parameters 01 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Functions - Parameters 02 - NoParens``() = x.DoNamedTest()
    [<Test>] member x.``Functions - Parameters 03 - ExistingAttribute``() = x.DoNamedTest()
    [<Test>] member x.``Functions - Parameters 04 - ExistingAttributes``() = x.DoNamedTest()
    [<Test>] member x.``Functions - Parameters 05 - Typed - ExistingAttribute``() = x.DoNamedTest()

    [<Test>] member x.``Methods - Parameters 01 - NoParens``() = x.DoNamedTest()
    [<Test>] member x.``Methods - Parameters 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Methods - Parameters 03 - ExistingAttribute``() = x.DoNamedTest()

type AddAttributeToMemberActionTest() =
    inherit FSharpContextActionExecuteTestBase<AddAttributeToMemberAction>()

    override x.ExtraPath = "addAttribute"

    [<Test>] member x.``Methods 01 - NoAttribute``() = x.DoNamedTest()
    [<Test>] member x.``Methods 02 - ExistingAttribute``() = x.DoNamedTest()

    [<Test>] member x.``Properties 01 - NoAttribute``() = x.DoNamedTest()
    [<Test>] member x.``Properties 02 - ExistingAttribute``() = x.DoNamedTest()

type AddAttributeToTypeActionTest() =
    inherit FSharpContextActionExecuteTestBase<AddAttributeToTypeAction>()

    override x.ExtraPath = "addAttribute"

    [<Test>] member x.``Types 01 - NoAttribute``() = x.DoNamedTest()
    [<Test>] member x.``Types 02 - ExistingAttribute``() = x.DoNamedTest()

type AddAttributeToModuleActionTest() =
    inherit FSharpContextActionExecuteTestBase<AddAttributeToModuleAction>()

    override x.ExtraPath = "addAttribute"

    [<Test>] member x.``Modules 01 - NoAttribute``() = x.DoNamedTest()
    [<Test>] member x.``Modules 02 - ExistingAttribute``() = x.DoNamedTest()