using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<ILanguageFSharpZone>
  {
  }
}