using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class TypeProvidersHost : OutOfProcessProtocolHostBase<ITypeProvider, RdTypeProvider>
  {
    private readonly RdTypeProviderProcessModel myProcessModel;
    private readonly IOutOfProcessProtocolHost<IProvidedNamespace, RdProvidedNamespace> myProvidedNamespacesHost;

    public TypeProvidersHost(ITypeProvider typeProvider, RdTypeProviderProcessModel processModel) : base(
      new TypeProviderEqualityComparer())
    {
      myProcessModel = processModel;
      myProvidedNamespacesHost = new ProvidedNamespacesHost(typeProvider);
      myRdTypeProviderProcessModel = new RdTypeProviderProcessModel();
      myRdTypeProviderProcessModel.GetNamespaces.Set(GetTypeProviderNamespaces);
    }

    protected override RdTypeProvider CreateRdModel(ITypeProvider providedNativeModel, int entityId)
      => new RdTypeProvider(entityId);

    private RdTask<RdProvidedNamespace[]> GetTypeProviderNamespaces(Lifetime lifetime, int providerId)
    {
      var typeProvider = GetEntity(providerId);

      var namespaces = typeProvider
        .GetNamespaces()
        .Select(t => myProvidedNamespacesHost.GetRdModel(t))
        .ToArray();
      return RdTask<RdProvidedNamespace[]>.Successful(namespaces);
    }
  }
}
