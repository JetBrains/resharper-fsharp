namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open System
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.Components
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.MSBuild
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.TestFramework
open JetBrains.TestFramework.Projects
open JetBrains.Util.Dotnet.TargetFrameworkIds
open NUnit.Framework

module AssemblyInfo =
    [<assembly: RequiresSTA>]
        do()

type FSharpTestAttribute() =
    inherit TestProjectFilePropertiesProvider(FSharpProjectFileType.FsExtension, MSBuildProjectUtil.CompileElement)

    let targetFrameworkId =
        TargetFrameworkId.Create(FrameworkIdentifier.NetFramework, new Version(4, 5, 1), ProfileIdentifier.Default)

    interface ITestPlatformProvider with
        member x.GetTargetFrameworkId() = targetFrameworkId

    interface ITestFileExtensionProvider with
        member x.Extension = FSharpProjectFileType.FsExtension

    interface ITestProjectPropertiesProvider with
        member x.GetProjectProperties(targetFrameworkIds, _) =
            FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)

[<SolutionComponent>]
type FSharpTestProjectOptionsProvider
        (lifetime: Lifetime, checkerService: FSharpCheckerService,
         scriptOptionsProvider: FSharpScriptOptionsProvider) as this =
    do
        checkerService.OptionsProvider <- this
        lifetime.OnTermination(fun _ -> checkerService.OptionsProvider <- Unchecked.defaultof<_>) |> ignore

    let getPath (sourceFile: IPsiSourceFile) = sourceFile.GetLocation().FullPath

    let getProjectOptions fileName references =
        { ProjectFileName = fileName + ".fsproj"
          ProjectId = None
          SourceFiles = [| fileName |]
          OtherOptions = Array.map ((+) "-r:") references
          ReferencedProjects = Array.empty
          IsIncompleteTypeCheckEnvironment = false
          UseScriptResolutionRules = false
          LoadTime = DateTime.Now
          OriginalLoadReferences = List.empty
          UnresolvedReferences = None
          ExtraProjectInfo = None
          Stamp = None }

    interface IHideImplementation<FSharpProjectOptionsProvider>
    
    interface IFSharpProjectOptionsProvider with
        member x.HasPairFile(sourceFile) = false

        member x.GetProjectOptions(sourceFile) =
            if sourceFile.LanguageType.Is<FSharpScriptProjectFileType>() then
                scriptOptionsProvider.GetScriptOptions(sourceFile) else

            let path = getPath sourceFile
            let projectOptions = getProjectOptions path [||] 
            Some projectOptions

        member x.GetParsingOptions(file) =
            let isExe =
                match file.GetProject() with
                | null -> false
                | project ->

                match project.ProjectProperties.BuildSettings with
                | :? IManagedProjectBuildSettings as buildSettings ->
                    buildSettings.OutputType = ProjectOutputType.CONSOLE_EXE
                | _ -> false

            { FSharpParsingOptions.Default with
                SourceFiles = [| getPath file |]
                IsExe = isExe }

        member x.GetFileIndex(_) = 0
        member x.ModuleInvalidated = new Signal<_>("Todo") :> _
