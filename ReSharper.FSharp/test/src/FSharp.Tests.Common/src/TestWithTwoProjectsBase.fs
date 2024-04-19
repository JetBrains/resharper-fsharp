namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open System
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework

[<AbstractClass; FSharpTest>]
type TestWithTwoProjectsBase(mainFileExtension: string, secondFileExtension: string) =
    inherit BaseTestWithSingleProject()

    override x.ProjectName = base.ProjectName + ".MainProject"

    member x.SecondProject = x.Solution.GetProjectByName(x.SecondProjectName)
    override x.SecondProjectName = base.ProjectName + "SecondProject"

    override x.DoTestSolution([<ParamArray>] _names: string[]) =
        let fileSets = FSharpTestUtil.createTestFileSets x mainFileExtension secondFileExtension
        x.DoTestSolution(fileSets)
