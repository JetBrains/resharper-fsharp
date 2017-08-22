using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Plugins.FSharp.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Cs
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<ILanguageFSharpZone>, IRequire<ICodeEditingZone>
  {
  }
}