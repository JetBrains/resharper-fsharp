using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.FSharp.Services
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IFSharpPluginZone>
  {
  }
}