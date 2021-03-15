namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Feature.Services.TypeNameHints
open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<HighlightOnly(typeof<TypeHintHighlighting>)>]
[<TestPackages("FSharp.Core")>]
[<TestSettingsKey(typeof<TypeNameHintsOptions>)>]
[<TestSetting(typeof<FSharpTypeHintOptions>, "ShowPipeReturnTypes", "false")>]
type InferredTypeHintStageTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/inferredTypeHints"

    [<Test; TestSettings("{ShowTypeNameHintsForLambdaExpressionParameters:All}")>]
    member x.``Function 01 - No arguments``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForLambdaExpressionParameters:All}")>]
    member x.``Function 02 - Curried arguments``() = x.DoNamedTest()

    [<Test>] member x.``Function 03 - Generic arguments``() = x.DoNamedTest()
    [<Test>] member x.``Function 04 - Tupled arguments``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForImplicitlyTypedVariables:All}")>]
    member x.``Function 05 - Type annotated - Curried``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForImplicitlyTypedVariables:All}")>]
    member x.``Function 06 - Type annotated - Tupled``() = x.DoNamedTest()

    [<Test>] member x.``Function 07 - Local function``() = x.DoNamedTest()
    [<Test>] member x.``Function 08 - Function arguments``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForPatternMatchingExpressions:All}")>]
    member x.``Function 09 - Pattern match argument``() = x.DoNamedTest()

    [<Test>] member x.``Function 10 - Arguments in parentheses``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForLambdaExpressionParameters:All}")>]
    member x.``Method 01 - No arguments``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForLambdaExpressionParameters:All}")>]
    member x.``Method 02 - Curried arguments``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForImplicitlyTypedVariables:All}")>]
    member x.``Method 03 - Type annotated - Curried``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForImplicitlyTypedVariables:All}")>]
    member x.``Method 04 - Type annotated - Tupled``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForPatternMatchingExpressions:All}")>]
    member x.``Pattern match 01 - Option``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForPatternMatchingExpressions:All}")>]
    member x.``Pattern match 02 - Record``() = x.DoNamedTest()

    [<Test>]
    member x.``Pattern match 03 - Wildcard``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForPatternMatchingExpressions:All}")>]
    member x.``Pattern match 04 - Function``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForPatternMatchingExpressions:All}")>]
    member x.``Pattern match 05 - Active pattern``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForPatternMatchingExpressions:All}")>]
    member x.``Pattern match 06 - Type match``() = x.DoNamedTest()

    [<Test; TestSettings("{ShowTypeNameHintsForPatternMatchingExpressions:All}")>]
    member x.``Pattern match 07 - Lists and arrays``() = x.DoNamedTest()

    [<Test; TestSettings("{HideTypeNameHintsForImplicitlyTypedVariablesWhenTypeIsEvident:All}")>]
    member x.``Value 01 - Literals``() = x.DoNamedTest()

    [<Test; TestSettings("{HideTypeNameHintsForImplicitlyTypedVariablesWhenTypeIsEvident:All}")>]
    member x.``Value 02 - Non literals``() = x.DoNamedTest()

    [<Test; TestSettings("{HideTypeNameHintsForImplicitlyTypedVariablesWhenTypeIsEvident:All}")>]
    member x.``Value 03 - Pattern match``() = x.DoNamedTest()

    [<Test>] member x.``Nested 01``() = x.DoNamedTest()
