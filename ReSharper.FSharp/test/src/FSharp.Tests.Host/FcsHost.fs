namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Host

open JetBrains.DataFlow
open JetBrains.Platform.RdFramework.Impl
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.Rider.Model

[<SolutionComponent>]
type FcsHost(lifetime: Lifetime, checkerService: FSharpCheckerService, solutionModel: SolutionModel) =
    do
        match solutionModel.TryGetCurrentSolution() with
        | null -> ()
        | solution ->

        match solution.FsharpCompilerServiceHost.ProjectChecked with
        | :? RdSignal<_> as signal ->
            signal.Async <- true
            let handler = fun (project, _) -> signal.Fire(project)
            let subscription = checkerService.Checker.ProjectChecked.Subscribe(handler)
            lifetime.AddAction(fun _ -> subscription.Dispose()) |> ignore
        | _ -> ()
