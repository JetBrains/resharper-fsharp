namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.FileTypes
open JetBrains.ReSharper.Feature.Services.CodeStructure
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpStructureTest() =
    inherit BaseTestWithSingleProject()

    override x.RelativeTestDataPath = "features/structure"

    [<Test>] member x.``Module - Top level 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top level 02 - In namespace``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested 02 - In namespace``() = x.DoNamedTest()

    [<Test>] member x.``Namespace 01``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 02 - Multiple``() = x.DoNamedTest()

    [<Test>] member x.``Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Record 01``() = x.DoNamedTest()

    [<Test>] member x.``Class bindings 01``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 01``() = x.DoNamedTest()

    override x.DoTest(_: Lifetime, project: IProject) =
        let items = project.GetSubItems(x.TestName)
        let projectFile = items.FirstOrDefault().As<IProjectFile>().NotNull()
        let sourceFile = projectFile.ToSourceFile().NotNull()

        let services = Shell.Instance.GetComponent<IProjectFileTypeServices>()
        let structureProvider = services.TryGetService<IProjectFileCodeStructureProvider>(sourceFile.LanguageType)
        let root = structureProvider.Build(sourceFile, CodeStructureOptions.Default)
        x.ExecuteWithGold(projectFile, root.Dump)
