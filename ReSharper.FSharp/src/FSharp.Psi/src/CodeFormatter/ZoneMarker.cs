using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Platform.RdFramework;
using JetBrains.Rider.Backend.Env;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.CodeFormatter
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IRdFrameworkZone>, IRequire<IRiderFeatureEnvironmentZone>
  {
  }
}