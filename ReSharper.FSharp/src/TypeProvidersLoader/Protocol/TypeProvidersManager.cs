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

    public TypeProvidersManager() : base(new TypeProviderEqualityComparer())
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

  internal class TypeProviderEqualityComparer : IEqualityComparer<ITypeProvider>
  {
    public bool Equals(ITypeProvider x, ITypeProvider y)
    {
      return x.GetType().FullName == y.GetType().FullName;
    }

    public int GetHashCode(ITypeProvider obj)
    {
      return obj.GetType().FullName.GetHashCode();
    }
  }
}
