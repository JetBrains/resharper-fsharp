using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class TypeProvidersHost : OutOfProcessProtocolHostBase<ITypeProvider, RdTypeProviderProcessModel>
  {
    private readonly IOutOfProcessProtocolHost<IProvidedNamespace, RdProvidedNamespace> myProvidedNamespacesHost;
    private readonly RdTypeProviderProcessModel myRdTypeProviderProcessModel;

    public TypeProvidersHost()
    {
      myProvidedNamespacesHost = new ProvidedNamespacesHost();
      myRdTypeProviderProcessModel = new RdTypeProviderProcessModel();
      myRdTypeProviderProcessModel.GetNamespaces.Set(GetTypeProviderNamespaces);
    }

    protected override RdTypeProviderProcessModel CreateRdModel(
      ITypeProvider providedNativeModel,
      ITypeProvider providedModelOwner) => myRdTypeProviderProcessModel;

    private RdTask<RdProvidedNamespace[]> GetTypeProviderNamespaces(Lifetime lifetime, int providerId)
    {
      var typeProvider = ProvidedModelsCache[providerId];
      var namespaces = typeProvider
        .GetNamespaces()
        .Select(t => myProvidedNamespacesHost.GetRdModel(t, typeProvider)).ToArray();
      return RdTask<RdProvidedNamespace[]>.Successful(namespaces);
    }
  }
}
