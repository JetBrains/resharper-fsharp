namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Host

open System.Collections.Generic
open System.Linq
open FSharp.Compiler.AbstractIL.Internal.Library
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.Rd.Tasks
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<SolutionComponent>]
type FSharpTestHost
        (lifetime: Lifetime, solution: ISolution, checkerService: FSharpCheckerService, sourceCache: FSharpSourceCache,
         itemsContainer: FSharpItemsContainer) =

    let dumpSingleProjectMapping _ =
        let projectMapping =
            itemsContainer.ProjectMappings.Values.SingleOrDefault().NotNull("Expected single project mapping.")
        projectMapping.DumpToString()

    let dumpSingleProjectLocalReferences _ =
        use cookie = ReadLockCookie.Create()

        let project = solution.GetAllProjects().Single(fun project -> not project.ProjectFileLocation.IsEmpty)
        let directory = project.Location.Directory

        let psiModule = solution.GetPsiServices().Modules.GetPsiModules(project).Single()

        let sourceFile =
            psiModule.SourceFiles
            |> Seq.find (fun sourceFile -> sourceFile.LanguageType.Is<FSharpProjectFileType>())

        solution.GetComponent<IFcsProjectProvider>().GetProjectOptions(sourceFile)
        |> Option.map (fun options ->
            options.OtherOptions
            |> Array.choose (fun o -> if o.StartsWith("-r:") then Some (o.Substring("-r:".Length)) else None)
            |> Array.map (fun s -> FileSystemPath.TryParse(s))
            |> Array.filter (fun p -> not p.IsEmpty && directory.IsPrefixOf(p))
            |> Array.map (fun p -> p.Name)
            |> List)
        |> Option.defaultWith (fun _ -> List())

    do
        let fsTestHost = solution.RdFSharpModel().FsharpTestHost

        // We want to get events published by background checker.
        checkerService.Checker.ImplicitlyStartBackgroundWork <- true

        let subscription = checkerService.Checker.ProjectChecked.Subscribe(fun (projectFilePath, _) ->
            fsTestHost.ProjectChecked(projectFilePath))
        lifetime.OnTermination(fun _ -> subscription.Dispose()) |> ignore

        fsTestHost.GetLastModificationStamp.Set(FileSystem.GetLastWriteTimeShim)
        fsTestHost.GetSourceCache.Set(sourceCache.GetRdFSharpSource)
        fsTestHost.DumpSingleProjectMapping.Set(dumpSingleProjectMapping)
        fsTestHost.DumpSingleProjectLocalReferences.Set(dumpSingleProjectLocalReferences)
