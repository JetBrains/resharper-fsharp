using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ProjectModel;

namespace JetBrains.Platform.ProjectModel.FSharp
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IProjectModelZone>
  {
  }
}