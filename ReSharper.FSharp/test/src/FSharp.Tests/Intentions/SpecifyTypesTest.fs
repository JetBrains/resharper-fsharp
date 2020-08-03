namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages("FSharp.Core")>]
type SpecifyTypesActionTest() =
    inherit FSharpContextActionExecuteTestBase<SpecifyTypesAction>()

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

    [<TestSetting(typeof<FSharpFormatSettingsKey>, "SpaceBeforeColon", "true")>]
    [<Test>] member x.``Function - Formatting - Add space``() = x.DoNamedTest()

    [<Test>] member x.``Value 01``() = x.DoNamedTest()


[<TestPackages("FSharp.Core")>]
type SpecifyTypesActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<SpecifyTypesAction>()

    override x.ExtraPath = "specifyTypes"

    [<Test>] member x.``Module - Name 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Name 02 - Attributes``() = x.DoNamedTest()

    [<Test>] member x.``Not available 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available 02 - Annotated``() = x.DoNamedTest()

    [<Test>] member x.``Class - member - 01``() = x.DoNamedTest()
