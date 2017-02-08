using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ProjectModel
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IProjectModelZone>
  {
  }
}