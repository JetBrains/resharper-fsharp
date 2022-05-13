using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class SolutionExtension
  {
    public static bool TryGetProvidedType(this ISolution solution, IClrTypeName name,
      out ProxyProvidedTypeWithContext providedType)
    {
      providedType = null;
      var extensionTypingProvider = solution.GetComponent<IProxyExtensionTypingProvider>();
      return extensionTypingProvider.TypeProvidersManager?.Context.ProvidedAbbreviations.TryGetValue(name.FullName,
        out providedType) ?? false;
    }
  }
}
