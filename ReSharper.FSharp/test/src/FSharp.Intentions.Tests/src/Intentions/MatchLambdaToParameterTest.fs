namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.TestFramework
open JetBrains.ReSharper.TestFramework.Components.Format
open NUnit.Framework

[<AssertCorrectTreeStructure>]
type MatchLambdaToParameterTest() =
    inherit FSharpContextActionExecuteTestBase<MatchLambdaExprToParameterAction>()

    override x.ExtraPath = "matchLambdaToParameter"

    override this.DoTest(lifetime: Lifetime, testProject: IProject) =
        let formatSettingsService = testProject.GetSolution().GetComponent<TestGlobalFormatSettingsService>()
        formatSettingsService.ChangeIndentSize(this.TestLifetime, FSharpLanguage.Instance, 4)

        base.DoTest(lifetime, testProject)

    [<Test>] member x.``Comment 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Comment 02``() = x.DoNamedTest()

    [<Test>] member x.``Names - Generic 01``() = x.DoNamedTest()
    [<Test>] member x.``Names - String 01``() = x.DoNamedTest()
    [<Test>] member x.``Names - Used 01``() = x.DoNamedTest()

    [<Test>] member x.``Single line 01``() = x.DoNamedTest()
    [<Test>] member x.``Single line 02``() = x.DoNamedTest()
