namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open System
open System.Linq
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application
open JetBrains.Application.Components
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.BuildTools
open JetBrains.ProjectModel.MSBuild.BuildTools
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

[<SolutionInstanceComponent>]
type MyTestSolutionToolset(lifetime: Lifetime, logger: ILogger) =
    inherit DefaultSolutionToolset(lifetime, logger)

    let changed = new Signal<_>(lifetime, "MySolutionToolset::Changed")

    let cli = DotNetCoreRuntimesDetector.DetectDotNetCoreRuntimes().FirstOrDefault().NotNull()
    let dotnetCoreToolset = DotNetCoreToolset(cli, cli.Sdks.FirstOrDefault().NotNull())

    let env = BuildToolEnvironment.Create(dotnetCoreToolset, null)
    let buildTool = DotNetCoreMsBuildProvider().Discover(env).FirstOrDefault().NotNull()

    interface ISolutionToolset with
        member x.GetBuildTool() = buildTool
        member x.Changed = changed :> _
