namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Host

open JetBrains.DataFlow
open JetBrains.Platform.RdFramework.Util
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Common.Shim.FileSystem
open JetBrains.Rider.Model
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library

[<SolutionComponent>]
type FcsHost
        (lifetime: Lifetime, solution: ISolution, checkerService: FSharpCheckerService,
         sourceCache: FSharpSourceCache) =
    do
        let fcsHost = solution.GetProtocolSolution().GetRdFSharpModel().FSharpCompilerServiceHost

        let projectChecked = fcsHost.ProjectChecked :?> IRdSignal<string>
        let subscription = checkerService.Checker.ProjectChecked.Subscribe(fun (projectFilePath, _) ->
            projectChecked.Fire(projectFilePath))
        lifetime.AddAction(fun _ -> subscription.Dispose()) |> ignore

        fcsHost.GetLastModificationStamp.Set(Shim.FileSystem.GetLastWriteTimeShim)
        fcsHost.GetSourceCache.Set(sourceCache.GetRdFSharpSource)
