namespace JetBrains.ReSharper.Plugins.FSharp

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ProjectModel.NuGet

[<ZoneDefinition(ZoneFlags.AutoEnable)>]
type IFSharpPluginZone =
    inherit IZone
    inherit IRequire<ILanguageFSharpZone>
    inherit IRequire<JetBrains.ReSharper.Psi.CSharp.ILanguageCSharpZone>
    inherit IRequire<PsiFeaturesImplZone>
    inherit IRequire<DaemonZone>
    inherit IRequire<INuGetZone>
    inherit IRequire<JetBrains.ReSharper.Daemon.Syntax.ISyntaxHighlightingZone>
    inherit IRequire<JetBrains.ReSharper.Features.ReSpeller.IReSpellerZone>
    inherit IRequire<JetBrains.ReSharper.Feature.Services.Navigation.NavigationZone>
    inherit IRequire<JetBrains.ReSharper.Feature.Services.ICodeEditingZone>


[<ZoneMarker>]
type ZoneMarker() =
    interface IRequire<IFSharpPluginZone>
