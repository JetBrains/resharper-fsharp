using System.Collections.Generic;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using JetBrains.Rd.Tasks;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class TypeProvidersManager : OutOfProcessProtocolManagerBase<ITypeProvider, RdTypeProvider>
  {
    private readonly IOutOfProcessProtocolManager<IProvidedNamespace, RdProvidedNamespace> myProvidedNamespacesManager;

    public TypeProvidersManager() : base(EqualityComparer<ITypeProvider>.Default)
    {
      myProvidedNamespacesManager = new ProvidedNamespacesManager();
    }

    protected override RdTypeProvider CreateProcessModel(ITypeProvider providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var tpProtocolModel = new RdTypeProvider();
      tpProtocolModel.GetNamespaces.Set((lifetime, _) => GetTypeProviderNamespaces(lifetime, providedNativeModel));

      return tpProtocolModel;
    }

    private RdTask<RdProvidedNamespace[]> GetTypeProviderNamespaces(Lifetime lifetime, ITypeProvider typeProvider)
    {
      var namespaces = typeProvider
        .GetNamespaces()
        .Select(t => myProvidedNamespacesManager.Register(t, typeProvider)).ToArray();
      return RdTask<RdProvidedNamespace[]>.Successful(namespaces);
    }
  }
}
