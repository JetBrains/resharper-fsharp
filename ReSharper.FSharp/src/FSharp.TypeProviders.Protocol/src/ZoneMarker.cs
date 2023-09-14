using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ProjectModel.NuGet;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<ILanguageFSharpZone>, IRequire<INuGetZone>, IRequire<IRiderModelZone>
  {
  }
}
