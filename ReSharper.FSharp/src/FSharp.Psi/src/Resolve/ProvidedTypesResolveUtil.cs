using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public static class ProvidedTypesResolveUtil
  {
    public static bool TryGetProvidedType(IPsiModule psiModule, IClrTypeName name,
      out ProxyProvidedTypeWithContext providedType)
    {
      providedType = null;
      var extensionTypingProvider = psiModule.GetSolution().GetComponent<IProxyExtensionTypingProvider>();
      return extensionTypingProvider.TypeProvidersManager?.Context.ProvidedAbbreviations.TryGet(psiModule, name,
        out providedType) ?? false;
    }
  }
}
