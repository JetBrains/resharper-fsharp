using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ProjectModel.NuGet;
using JetBrains.ReSharper.Daemon.Syntax;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Features.ReSpeller;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  [ZoneDefinition(ZoneFlags.AutoEnable)]
  public interface IFSharpPsiZone : IZone,
    IRequire<ILanguageFSharpZone>,
    IRequire<ILanguageCSharpZone>,
    IRequire<ICodeEditingZone>,
    IRequire<PsiFeaturesImplZone>;

  [ZoneDefinition(ZoneFlags.AutoEnable)]
  public interface IFSharpPluginZone : IZone,
    IRequire<IFSharpPsiZone>,
    IRequire<DaemonZone>,
    IRequire<INuGetZone>,
    IRequire<IReSpellerZone>,
    IRequire<ISyntaxHighlightingZone>,
    IRequire<NavigationZone>;
}
