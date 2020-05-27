namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Host

open System.Linq
open FSharp.Compiler.AbstractIL.Internal.Library
open JetBrains.Core
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.Rd.Tasks
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Rider.Model

[<SolutionComponent>]
type TestFcsHost
        (lifetime: Lifetime, solution: ISolution, checkerService: FSharpCheckerService, sourceCache: FSharpSourceCache,
         itemsContainer: FSharpItemsContainer, fcsProjectProvider: FcsProjectProvider) =

    let dumpSingleProjectMapping (_: Unit) =
        let projectMapping =
            itemsContainer.ProjectMappings.Values.SingleOrDefault().NotNull("Expected single project mapping.")
        projectMapping.DumpToString()

    do
        let fcsHost = solution.RdFSharpModel().FcsHost

        // We want to get events published by background checker.
        checkerService.Checker.ImplicitlyStartBackgroundWork <- true

        let subscription = checkerService.Checker.ProjectChecked.Subscribe(fun (projectFilePath, _) ->
            fcsHost.ProjectChecked(projectFilePath))
        lifetime.OnTermination(fun _ -> subscription.Dispose()) |> ignore

        fcsProjectProvider.FcsProjectInvalidated.Advise(lifetime, fun (psiModule: IPsiModule) ->
            let rdFcsProject = RdFcsProject(psiModule.Name, psiModule.TargetFrameworkId.PresentableString)
            fcsHost.FcsProjectInvalidated(rdFcsProject))

        fcsHost.GetLastModificationStamp.Set(Shim.FileSystem.GetLastWriteTimeShim)
        fcsHost.GetSourceCache.Set(sourceCache.GetRdFSharpSource)
        fcsHost.DumpSingleProjectMapping.Set(dumpSingleProjectMapping)
