using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Platform.RdFramework;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IRdFrameworkZone>, IRequire<ISinceClr4HostZone>
  {
  }
}