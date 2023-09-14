using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Platform.RdFramework;
using JetBrains.RdBackend.Common.Env;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.Rider.Backend.Env;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.ContextHighlighters
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<DaemonEngineZone>, IRequire<DaemonZone>, IRequire<ILanguageCSharpZone>, IRequire<IRdFrameworkZone>, IRequire<IResharperHostCoreFeatureZone>, IRequire<IRiderFeatureEnvironmentZone>, IRequire<ISinceClr4HostZone>
  {
  }
}