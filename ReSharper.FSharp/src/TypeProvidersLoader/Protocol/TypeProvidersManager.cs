using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using JetBrains.Rd.Tasks;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class TypeProvidersManager: IOutOfProcessProtocolManager<ITypeProvider, RdTypeProvider>
  {
    private readonly IOutOfProcessProtocolManager<IProvidedNamespace, RdProvidedNamespace> myProvidedNamespacesManager;

    public TypeProvidersManager(IOutOfProcessProtocolManager<IProvidedNamespace, RdProvidedNamespace> providedNamespacesManager)
    {
      myProvidedNamespacesManager = providedNamespacesManager;
    }

    public RdTypeProvider Register(ITypeProvider providedMethod)
    {
      var tpProtocolModel = new RdTypeProvider();
      
      //tpProtocolModel.GetGeneratedAssemblyContents.Set((lifetime, ) => GetGeneratedAssemblyContents(lifetime, typeProvider));
      tpProtocolModel.GetNamespaces.Set((lifetime, _) => GetTypeProviderNamespaces(lifetime, providedMethod));
      //tpProtocolModel.ApplyStaticArguments.Set(ApplyStaticArguments);
      //tpProtocolModel.GetStaticParameters.Set(GetStaticParameters);

      return tpProtocolModel;
    }

    private RdTask<RdProvidedNamespace[]> GetTypeProviderNamespaces(Lifetime lifetime, ITypeProvider typeProvider)
    {
      var namespaces = typeProvider.GetNamespaces().Select(myProvidedNamespacesManager.Register).ToArray();
      return RdTask<RdProvidedNamespace[]>.Successful(namespaces);
    }
  }
}
