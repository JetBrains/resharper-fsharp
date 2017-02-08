using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Psi.FSharp.LanguageService
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<ILanguageFSharpZone>
  {
  }
}