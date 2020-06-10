namespace JetBrains.ReSharper.Plugins.FSharp.Common.Tests.Stubs

open JetBrains.Application.Components
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp

[<SolutionComponent>]
type TestFcsReactorMonitor() =
    interface IHideImplementation<FcsReactorMonitor>
