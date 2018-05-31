namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Host

open JetBrains.DataFlow
open JetBrains.Platform.RdFramework.Util
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

        match solution.GetRdFSharpModel().FSharpCompilerServiceHost.ProjectChecked with
        | :? IRdSignal<_> as signal ->
            let subscription = checkerService.Checker.ProjectChecked.Subscribe(fun (project, _) -> signal.Fire(project))
            lifetime.AddAction(fun _ -> subscription.Dispose()) |> ignore
        | _ -> ()
