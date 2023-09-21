using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Platform.RdFramework;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.CodeFormatter
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IRdFrameworkZone>
  {
  }
}