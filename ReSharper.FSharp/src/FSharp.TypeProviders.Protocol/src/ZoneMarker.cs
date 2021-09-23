using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.NuGet;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IProjectModelZone>, IRequire<INuGetZone>
  {
  }
}
