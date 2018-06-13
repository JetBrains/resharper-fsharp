namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open System
open JetBrains.Application.Components
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.TestFramework
open JetBrains.Util.Dotnet.TargetFrameworkIds
open Microsoft.FSharp.Compiler.SourceCodeServices
open NUnit.Framework

module AssemblyInfo =
    [<assembly: RequiresSTA>]
        do()

type FSharpTestAttribute() =
    inherit TestPackagesAttribute()

    let targetFrameworkId =
        TargetFrameworkId.Create(FrameworkIdentifier.NetFramework, new Version(4, 5, 1), ProfileIdentifier.Default)

    interface ITestPlatformProvider with
        member x.GetTargetFrameworkId() = targetFrameworkId

[<SolutionComponent>]
type FSharpTestProjectOptionsProvider(lifetime: Lifetime, checkerService: FSharpCheckerService) as this =
    do
        checkerService.OptionsProvider <- this
        lifetime.AddAction(fun _ -> checkerService.OptionsProvider <- Unchecked.defaultof<_>) |> ignore

    let getPath (sourceFile: IPsiSourceFile) = sourceFile.GetLocation().FullPath

    let getProjectOptions fileName references =
        { ProjectFileName = fileName + ".fsproj"
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
