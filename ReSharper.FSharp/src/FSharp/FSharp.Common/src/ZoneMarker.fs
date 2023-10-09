namespace JetBrains.ReSharper.Plugins.FSharp

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel.NuGet
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.ReSharper.Daemon.Syntax
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.Navigation
open JetBrains.ReSharper.Features.ReSpeller
open JetBrains.ReSharper.Psi.CSharp
open JetBrains.ReSharper.Resources.Shell

[<ZoneDefinition(ZoneFlags.AutoEnable)>]
type IFSharpPluginZone =
    inherit IZone
    inherit IRequire<DaemonZone>
    inherit IRequire<ICodeEditingZone>
    inherit IRequire<ILanguageCSharpZone>
    inherit IRequire<ILanguageFSharpZone>
    inherit IRequire<INuGetZone>
    inherit IRequire<IReSpellerZone>
    inherit IRequire<ISyntaxHighlightingZone>
    inherit IRequire<NavigationZone>
    inherit IRequire<PsiFeaturesImplZone>


[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IFSharpPluginZone>
