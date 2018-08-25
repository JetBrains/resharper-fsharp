namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Common

open System
open JetBrains.Application
open JetBrains.Application.Components
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp.Common
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Services.ContextActions
open JetBrains.ReSharper.Psi
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

[<SolutionComponent>]
type FsiSessionsHostStub() =
    interface IHideImplementation<FsiSessionsHost>


[<SolutionComponent>]
type FSharpProjectOptionsBuilderStub() =
    interface IHideImplementation<FSharpProjectOptionsBuilder>


[<ShellComponent>]
type FSharpFileServiceStub() =
    interface IHideImplementation<FSharpFileService>

    interface IFSharpFileService with
        member x.IsScratchFile(_) = false
        member x.IsScriptLike(_) = false


[<SolutionComponent>]
type FsiDetectorStub() =
    interface IHideImplementation<FsiDetector>

    interface IFsiDetector with
        member x.GetSystemFsiDirectoryPath() = FileSystemPath.Empty


/// Used to add assemblies to R# subplatfrom at runtime
type AddAssembliesToSubplatform() =
    let _ = FsiSessionsHostStub
    let _ = FSharpProjectLoadTargetsAnalyzer()


[<SolutionComponent>]
type FSharpTestProjectOptionsProvider(lifetime: Lifetime, checkerService: FSharpCheckerService) as this =
    do
        checkerService.OptionsProvider <- this
        lifetime.AddAction(fun _ -> checkerService.OptionsProvider <- Unchecked.defaultof<_>) |> ignore

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
        member x.HasPairFile(file) = false
        member x.GetProjectOptions(file) =
            let path = getPath file
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
