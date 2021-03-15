using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Host.Product;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IRiderProductEnvironmentZone>
  {
  }
}
