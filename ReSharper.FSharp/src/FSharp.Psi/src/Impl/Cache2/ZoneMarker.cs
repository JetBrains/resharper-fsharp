using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.RdBackend.Common.Env;
using JetBrains.Rider.Backend.Env;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IResharperHostCoreFeatureZone>, IRequire<IRiderFeatureEnvironmentZone>
  {
  }
}