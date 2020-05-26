namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open System
open System.Threading
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.Components
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.MSBuild
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.TestFramework
open JetBrains.TestFramework.Projects
open JetBrains.Util.Dotnet.TargetFrameworkIds
open Moq
open NUnit.Framework

[<assembly: Apartment(ApartmentState.STA)>]
do()

type FSharpTestAttribute(extension) =
    inherit TestProjectFilePropertiesProvider(extension, MSBuildProjectUtil.CompileElement)

    let targetFrameworkId =
        TargetFrameworkId.Create(FrameworkIdentifier.NetFramework, Version(4, 5, 1), ProfileIdentifier.Default)

    new () =
        FSharpTestAttribute(FSharpProjectFileType.FsExtension)

    interface ITestPlatformProvider with
        member x.GetTargetFrameworkId() = targetFrameworkId

    interface ITestFileExtensionProvider with
        member x.Extension = extension

    interface ITestProjectPropertiesProvider with
        member x.GetProjectProperties(targetFrameworkIds, _) =
            FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)


type FSharpSignatureTestAttribute() =
    inherit FSharpTestAttribute(FSharpSignatureProjectFileType.FsiExtension)


type FSharpScriptTestAttribute() =
    inherit FSharpTestAttribute(FSharpScriptProjectFileType.FsxExtension)


[<SolutionComponent>]
type TestFcsProjectBuilder(checkerService: FSharpCheckerService, logger: ILogger) =
    inherit FcsProjectBuilder(checkerService, Mock<_>().Object, logger)

    override x.GetProjectItemsPaths(_, _) = [||]

    interface IHideImplementation<FcsProjectBuilder>


[<SolutionComponent>]
type TestFcsProjectProvider
        (lifetime: Lifetime, checkerService: FSharpCheckerService, fcsProjectBuilder: FcsProjectBuilder,
         scriptFcsProjectProvider: IScriptFcsProjectProvider) as this =
    do
        checkerService.FcsProjectProvider <- this
        lifetime.OnTermination(fun _ -> checkerService.FcsProjectProvider <- Unchecked.defaultof<_>) |> ignore

    let getProjectOptions (sourceFile: IPsiSourceFile) =
        let fcsProject = fcsProjectBuilder.BuildFcsProject(sourceFile.PsiModule, sourceFile.GetProject())
        Some { fcsProject.ProjectOptions with SourceFiles = [| sourceFile.GetLocation().FullPath |] }

    interface IHideImplementation<FcsProjectProvider>
    
    interface IFcsProjectProvider with
        member x.HasPairFile _ = false

        member x.GetProjectOptions(sourceFile) =
            if sourceFile.LanguageType.Is<FSharpScriptProjectFileType>() then
                scriptFcsProjectProvider.GetScriptOptions(sourceFile) else

            getProjectOptions sourceFile

        member x.GetParsingOptions(sourceFile) =
            if isNull sourceFile then sandboxParsingOptions else

            let isScript = sourceFile.LanguageType.Is<FSharpScriptProjectFileType>()

            let isExe =
                match sourceFile.GetProject() with
                | null -> false
                | project ->

                let targetFrameworkId = sourceFile.PsiModule.TargetFrameworkId
                match project.ProjectProperties.ActiveConfigurations.TryGetConfiguration(targetFrameworkId) with
                | :? IManagedProjectConfiguration as cfg ->
                    cfg.OutputType = ProjectOutputType.CONSOLE_EXE
                | _ -> false

            { FSharpParsingOptions.Default with
                SourceFiles = [| sourceFile.GetLocation().FullPath |]
                IsExe = isExe
                IsInteractive = isScript }

        member x.GetFileIndex _ = 0
        member x.ModuleInvalidated = new Signal<_>("Todo") :> _

        member x.InvalidateReferencesToProject _ = false
        member x.HasFcsProjects = false
