using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Platform.RdFramework;
using JetBrains.RdBackend.Common.Env;
using JetBrains.Rider.Backend.Env;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IRdFrameworkZone>, IRequire<IResharperHostCoreFeatureZone>, IRequire<IRiderFeatureEnvironmentZone>, IRequire<ISinceClr4HostZone>
  {
  }
}