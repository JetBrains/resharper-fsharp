using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  [ZoneDefinition(ZoneFlags.AutoEnable)]
  public interface ILanguageFSharpZone : IClrPsiLanguageZone
  {
  }
}