namespace JetBrains.ReSharper.Plugins.FSharp.Tests

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
type TestWithTwoProjectsBase(mainFileExtension: string, secondFileExtension: string) =
    inherit BaseTestWithSingleProject()

    override x.ProjectName = base.ProjectName + ".MainProject"

    member x.SecondProject = x.Solution.GetProjectByName(x.SecondProjectName)
    override x.SecondProjectName = base.ProjectName + "SecondProject"

    abstract DoTest: mainProject: IProject * secondProject: IProject -> unit

    override x.DoTest(_: Lifetime, project: IProject) =
        x.AddProjectReference(project)
        x.DoTest(project, x.SecondProject)

    member x.AddProjectReference(project: IProject) =
        let lock = x.Locks.UsingWriteLock()
        try
            let progressIndicator = NullProgressIndicator.Create()
            use cookie = x.Solution.CreateTransactionCookie(DefaultAction.Commit, "Add reference", progressIndicator)
            cookie.AddModuleReference(project, x.SecondProject, project.GetSingleTargetFrameworkId()) |> ignore
        finally
            lock.Dispose()

    member x.CreateProjectDescriptor(name, filePath, libs, guid) =
        let targetFrameworkId = x.GetTargetFrameworkId()
        x.CreateProjectDescriptor(targetFrameworkId, name, [| filePath |], libs, guid)

    override x.DoTestSolution([<ParamArray>] _names: string[]) =
        let baseFilePath = x.TestDataPath / x.TestName
        let mainFilePath = baseFilePath.ChangeExtension(mainFileExtension)
        let secondFilePath = baseFilePath.ChangeExtension(secondFileExtension)

        let descriptors = Dictionary()
        let libs = x.GetReferencedAssemblies(x.GetTargetFrameworkId()).Distinct()

        let mainDescriptor = x.CreateProjectDescriptor(x.ProjectName, mainFilePath, libs, x.ProjectGuid)
        let secondDescriptor = x.CreateProjectDescriptor(x.SecondProjectName, secondFilePath, libs, x.SecondProjectGuid)

        descriptors.Add(mainDescriptor.First, mainDescriptor.Second)
        descriptors.Add(secondDescriptor.First, secondDescriptor.Second)

        let solutionFilePath =
            let path = FileSystemPath.TryParse(x.SolutionFileName)
            if not (path.IsNullOrEmpty()) && not path.IsAbsolute then x.TestDataPath / path.AsRelative()
            else path

        let cfg = BaseTestWithSolution.TestSolutionConfiguration(solutionFilePath, descriptors)
        BaseTestWithSingleProject.ProcessSdkReferences(cfg, x.ShellInstance.GetComponent<ISdkManager>().Kits)

        x.DoTestSolution((fun _ -> cfg), (null: Func<Action<Lifetime>, Action<Lifetime>>))
