using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Platform.RdFramework;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IFSharpPluginZone>, IRequire<IRdFrameworkZone>, IRequire<IRiderModelZone>
  {
  }
}
