namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Feature.Services.ParameterNameHints
open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestReferences("System.Drawing")>]
[<HighlightOnly(typeof<ParameterNameHintHighlighting>)>]
[<TestSettingsKey(typeof<ParameterNameHintsOptions>)>]
type ParameterNameHintStageTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/parameterNameHints"

    [<Test>] member x.``App 01 - Multiple literals``() = x.DoNamedTest()
    [<Test>] member x.``App 02 - Some literals``() = x.DoNamedTest()
    [<Test>] member x.``App 03 - Parentheses``() = x.DoNamedTest()
    [<Test>] member x.``App 04 - Ignore binary eq``() = x.DoNamedTest()
    [<Test>] member x.``App 05 - After constructor``() = x.DoNamedTest()
    [<Test>] member x.``App 06 - Ignored``() = x.DoNamedTest()
    [<Test>] member x.``Constructor 01`` () = x.DoNamedTest()

    [<Test; TestSettings("{HideForMethodsWithSameNamedNumberedParameters:All}")>]
    member x.``Toggleable 01 - Parameters only differ by numbered suffix`` () = x.DoNamedTest()

    [<Test; TestSettings("{ShowForNonLiteralsInCaseOfMultipleParametersWithSameName:All}")>]
    member x.``Toggleable 02 - Non literals with multiple parameters of same type`` () = x.DoNamedTest()
