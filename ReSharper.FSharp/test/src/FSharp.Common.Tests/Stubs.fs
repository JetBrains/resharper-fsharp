namespace JetBrains.ReSharper.Plugins.FSharp.Common.Tests.Stubs

open JetBrains.Application
open JetBrains.Application.Components
open JetBrains.ReSharper.Plugins.FSharp

[<ShellComponent>]
type TestFcsReactorMonitor() =
    inherit FcsReactorMonitorStub()

    interface IHideImplementation<FcsReactorMonitor>
