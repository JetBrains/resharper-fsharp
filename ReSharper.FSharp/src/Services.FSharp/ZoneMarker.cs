using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi.FSharp;

namespace JetBrains.ReSharper.Feature.Services.FSharp
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<ILanguageFSharpZone>
  {
  }
}