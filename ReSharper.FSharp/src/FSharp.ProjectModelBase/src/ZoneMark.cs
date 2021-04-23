using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IProjectModelZone>
  {
  }
}
