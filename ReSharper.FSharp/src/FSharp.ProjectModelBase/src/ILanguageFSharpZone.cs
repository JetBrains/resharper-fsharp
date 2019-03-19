using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  [ZoneDefinition]
  public interface ILanguageFSharpZone : IClrPsiLanguageZone
  {
  }
}