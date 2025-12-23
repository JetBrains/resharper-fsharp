namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open System.Linq
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.FeaturesTestFramework.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

type DeconstructPatternTest() =
    inherit FSharpContextActionExecuteTestBase<DeconstructPatternContextAction>()

    override this.ExtraPath = "deconstruct"

    override this.DoTest(lifetime: Lifetime, testProject: IProject) =
        let popupMenu = testProject.GetSolution().GetComponent<TestWorkflowPopupMenu>()
        popupMenu.SetTestData(lifetime, fun _ occurrences _ _ _ -> occurrences.FirstOrDefault())
        base.DoTest(lifetime, testProject)

    [<Test>] member x.``KeyValuePair 01``() = x.DoNamedTest()
    [<Test>] member x.``KeyValuePair 02``() = x.DoNamedTest()

    [<Test>] member x.``Tuple - Accessor 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Lambda 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Lambda 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Lambda 03 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Decl 02 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Decl 03 - Abbreviation``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Expr 02 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Expr 03 - Used``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Match 02 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Member 01 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Member - Add parens 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Member - Add parens 02``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Member - Add parens 03``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Member - Add parens 04``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Parameter owner 01``() = x.DoNamedTest()
    [<Test; Explicit("Enable .NET 5 in tests")>] member x.``Tuple - Struct 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Wild - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Wild - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Parameter - Used names 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Parameter - Used names 02``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Parameter - Used names 03``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Parameter - Used names 04``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Parameter - Used names 05``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Parameter - Used names 06``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Parameter - Used names 07``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Tuple - Parameter - Used names 08``() = x.DoNamedTest()
    [<Explicit("Fix mapping for local type parameters")>]
    [<Test>] member x.``Tuple - Parameter - Used names 09 - Local``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Parameter - Used names 10``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Used names 01``() = x.DoNamedTest()

    [<Test>] member x.``Union case - Single - Import 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Import 02``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Import 03``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Import 04``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Import 05``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Import 06``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Import 07``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Import 08``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Let 02``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Rqa 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Rqa 02 - Import``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Rqa 03 - Rqa module``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Rqa 04 - Escaped``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Used 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Used 02 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Used 03``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single 02 - Escaped``() = x.DoNamedTest()
    [<TestSetting(typeof<FSharpFormatSettingsKey>, "SpaceBeforeUppercaseInvocation", "true")>]
    [<Test>] member x.``Union case - Single 03 - Space``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single 04 - Arg``() = x.DoNamedTest()

    [<Test>] member x.``Union case fields - Generic 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields - Generic 02``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields - Generic 03 - Array``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields 02``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields 03``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields 04 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields 05 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Not available - Constructor param 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Union case - Single no fields``() = x.DoNamedTest()
