namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open System.Linq
open System.Collections.Generic
open JetBrains.Application.Components
open JetBrains.Application.Progress
open JetBrains.Application.Threading
open JetBrains.DocumentManagers.Transactions
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Impl.Sdk
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open JetBrains.Util

[<AbstractClass; FSharpTest>]
type TestWithTwoProjectsBase() =
    inherit BaseTestWithSingleProject()

    abstract MainFileExtension: string
    abstract SecondFileExtension: string

    override x.ProjectName = base.ProjectName + ".MainProject"

    member x.SecondProject = x.Solution.GetProjectByName(x.SecondProjectName)
    override x.SecondProjectName = base.ProjectName + "SecondProject"

    abstract DoTest: mainProject: IProject * secondProject: IProject -> unit

    override x.DoTest(_: Lifetime, project: IProject) =
        x.AddProjectReference(project)
        x.DoTest(project, x.SecondProject)

    member x.AddProjectReference(project: IProject) =
        use lock = x.Locks.UsingWriteLock()
        let progressIndicator = NullProgressIndicator.Create()
        use cookie = x.Solution.CreateTransactionCookie(DefaultAction.Commit, "Add reference", progressIndicator)
        cookie.AddModuleReference(project, x.SecondProject, project.GetSingleTargetFrameworkId()) |> ignore

    member x.CreateProjectDescriptor(name, filePath, libs, guid) =
        let targetFrameworkId = x.GetTargetFrameworkId()
        x.CreateProjectDescriptor(targetFrameworkId, name, [| filePath |], libs, guid)

    override x.DoTestSolution([<ParamArray>] _names: string[]) =
        let baseFilePath = x.TestDataPath / x.TestName
        let mainFile = baseFilePath.ChangeExtension(x.MainFileExtension)
        let secondFile = baseFilePath.ChangeExtension(x.SecondFileExtension)

        let descriptors = Dictionary()
        let libs = x.GetReferencedAssemblies(x.GetTargetFrameworkId()).Distinct()

        let mainDescriptor = x.CreateProjectDescriptor(x.ProjectName, mainFile, libs, x.ProjectGuid)
        let secondDescriptor = x.CreateProjectDescriptor(x.SecondProjectName, secondFile, libs, x.SecondProjectGuid)

        descriptors.Add(mainDescriptor.First, mainDescriptor.Second)
        descriptors.Add(secondDescriptor.First, secondDescriptor.Second)

        let solutionFilePath =
            let path = FileSystemPath.TryParse(x.SolutionFileName)
            if not (path.IsNullOrEmpty()) && not path.IsAbsolute then x.TestDataPath / path.AsRelative()
            else path

        let cfg = BaseTestWithSolution.TestSolutionConfiguration(solutionFilePath, descriptors)
        BaseTestWithSingleProject.ProcessSdkReferences(cfg, x.ShellInstance.GetComponent<ISdkManager>().Kits)

        x.DoTestSolution((fun _ -> cfg), (null: Func<Action<Lifetime>, Action<Lifetime>>))
