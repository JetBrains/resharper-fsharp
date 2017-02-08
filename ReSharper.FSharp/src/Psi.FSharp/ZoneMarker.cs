using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Psi.FSharp
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<ILanguageFSharpZone>
  {
  }
}