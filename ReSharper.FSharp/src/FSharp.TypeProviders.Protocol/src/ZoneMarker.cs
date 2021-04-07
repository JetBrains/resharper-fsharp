using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Rider.Backend.Product;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IRiderProductEnvironmentZone>
  {
  }
}
