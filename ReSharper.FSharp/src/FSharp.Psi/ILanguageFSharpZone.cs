using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  [ZoneDefinition]
  public interface ILanguageFSharpZone : IClrPsiLanguageZone
  {
  }
}