﻿namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages(FSharpCorePackage)>]
type FunctionReturnTypeAnnotationActionTest() =
    inherit FSharpContextActionExecuteTestBase<FunctionReturnTypeAnnotationAction>()

    override x.ExtraPath = "specifyReturnTypes"

    [<Test>] member x.``Function 01 - Unit to unit``() = x.DoNamedTest()
    [<Test>] member x.``Function 02 - Recursive``() = x.DoNamedTest()
    [<Test>] member x.``Function 03 - Generic types``() = x.DoNamedTest()
    [<Test>] member x.``Function 04 - Generalized``() = x.DoNamedTest()

    [<Test>] member x.``Function - Local 01``() = x.DoNamedTest()

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


[<TestPackages(FSharpCorePackage)>]
type FunctionReturnTypeAnnotationActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<FunctionReturnTypeAnnotationAction>()

    override x.ExtraPath = "specifyReturnTypes"

    [<Test>] member x.``Let bindings - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Let bindings - Module 01``() = x.DoNamedTest()

    [<Test>] member x.``Class - member - 01``() = x.DoNamedTest()

[<TestPackages(FSharpCorePackage)>]
type FunctionArgumentTypesAnnotationActionTest() =
    inherit FSharpContextActionExecuteTestBase<FunctionArgumentTypesAnnotationAction>()

    override x.ExtraPath = "specifyArgumentTypes"

    [<Test>] member x.``Function - Parameters 01 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 03 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters 04 - Nested tuple``() = x.DoNamedTest()

    [<Test>] member x.``Function 01 - Recursive``() = x.DoNamedTest()
    [<Test>] member x.``Function 02 - Generic types``() = x.DoNamedTest()
    [<Test>] member x.``Function 03 - Specified return``() = x.DoNamedTest()
    [<Test>] member x.``Function 04 - Generalized``() = x.DoNamedTest()

    [<Test>] member x.``Function - Local 01``() = x.DoNamedTest()

    [<Test>] member x.``Function - Parameters - Pattern 01 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 02 - Wild``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 03 - List``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 04 - As``() = x.DoNamedTest()
    [<Test>] member x.``Function - Parameters - Pattern 05 - Param owner``() = x.DoNamedTest()

    [<TestSetting(typeof<FSharpFormatSettingsKey>, "SpaceBeforeColon", "true")>]
    [<Test>] member x.``Function - Formatting - Add space``() = x.DoNamedTest()
    
    [<Test>] member x.``Function - Caret on let binding``() = x.DoNamedTest()


[<TestPackages(FSharpCorePackage)>]
type FunctionArgumentTypesAnnotationActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<FunctionArgumentTypesAnnotationAction>()

    override x.ExtraPath = "specifyArgumentTypes"

    [<Test>] member x.``Let bindings - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Let bindings - Module 01``() = x.DoNamedTest()

    [<Test>] member x.``Class - member - 01``() = x.DoNamedTest()
