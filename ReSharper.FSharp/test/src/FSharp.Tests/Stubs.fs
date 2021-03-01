namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open JetBrains.Application
open JetBrains.Application.Components
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

[<SolutionComponent>]
type FsiSessionsHostStub() =
    interface IHideImplementation<FsiHost>

[<ShellComponent>]
type FSharpFileServiceStub() =
    interface IHideImplementation<FSharpFileService>

    interface IFSharpFileService with
        member x.IsScratchFile _ = false
        member x.IsScriptLike _ = false

[<ShellComponent>]
type TestFcsReactorMonitor() =
    inherit FcsReactorMonitorStub()

    interface IHideImplementation<FcsReactorMonitor>
