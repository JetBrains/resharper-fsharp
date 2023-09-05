using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ProjectModel;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IProjectModelZone>, IRequire<IRiderModelZone>
  {
  }
}
