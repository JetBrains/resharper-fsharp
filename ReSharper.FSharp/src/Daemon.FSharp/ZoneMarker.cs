using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<ILanguageFSharpZone>, IRequire<DaemonZone>
  {
  }
}