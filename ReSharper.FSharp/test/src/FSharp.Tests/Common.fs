namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open System
open JetBrains.Application.Components
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.ProjectModel
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
        lifetime.AddAction(fun _ -> checkerService.OptionsProvider <- null) |> ignore

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
        member x.GetProjectOptions(file) = getProjectOptions (getPath file) [||] |> Some
        member x.GetParsingOptions(file) = { FSharpParsingOptions.Default with SourceFiles = [| getPath file |] }
        member x.HasPairFile(file) = false
