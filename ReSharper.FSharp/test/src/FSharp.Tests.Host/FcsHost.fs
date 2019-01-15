namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Host

open System.Linq
open JetBrains.DataFlow
open JetBrains.Platform.RdFramework
open JetBrains.Platform.RdFramework.Util
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Common.Shim.FileSystem
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectItems.ItemsContainer
open JetBrains.Rider.Model
open JetBrains.Util
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library

[<SolutionComponent>]
type FcsHost
        (lifetime: Lifetime, solution: ISolution, checkerService: FSharpCheckerService,
         sourceCache: FSharpSourceCache, itemsContainer: FSharpItemsContainer) =

    let dumpSingleProjectMapping (rdVoid: RdVoid) =
        let projectMapping =
            itemsContainer.ProjectMappings.Values.SingleOrDefault().NotNull("Expected single project mapping.")
        projectMapping.DumpToString()

    do
        let fcsHost = solution.GetProtocolSolution().GetRdFSharpModel().FSharpCompilerServiceHost

        // We want to get events published by background checker.
        checkerService.Checker.ImplicitlyStartBackgroundWork <- true
        
        let projectChecked = fcsHost.ProjectChecked :?> IRdSignal<string>
        let subscription = checkerService.Checker.ProjectChecked.Subscribe(fun (projectFilePath, _) ->
            projectChecked.Fire(projectFilePath))
        lifetime.OnTermination(fun _ -> subscription.Dispose()) |> ignore

        fcsHost.GetLastModificationStamp.Set(Shim.FileSystem.GetLastWriteTimeShim)
        fcsHost.GetSourceCache.Set(sourceCache.GetRdFSharpSource)
        fcsHost.DumpSingleProjectMapping.Set(dumpSingleProjectMapping)
