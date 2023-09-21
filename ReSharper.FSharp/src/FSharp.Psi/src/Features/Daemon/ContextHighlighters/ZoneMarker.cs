using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Platform.RdFramework;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.ContextHighlighters
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<DaemonEngineZone>, IRequire<DaemonZone>, IRequire<ILanguageCSharpZone>, IRequire<IRdFrameworkZone>, IRequire<ISinceClr4HostZone>
  {
  }
}