using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IProjectModelZone>
  {
  }
}
