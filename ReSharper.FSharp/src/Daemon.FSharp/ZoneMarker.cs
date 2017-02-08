using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.FSharp;
using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Daemon.FSharp
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<ILanguageFSharpZone>, IRequire<DaemonZone>
  {
  }
}