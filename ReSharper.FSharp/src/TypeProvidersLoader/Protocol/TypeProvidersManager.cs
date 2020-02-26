using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using JetBrains.Rd.Tasks;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class TypeProvidersManager: IOutOfProcessProtocolManager<ITypeProvider, RdTypeProvider>
  {
    private IOutOfProcessProtocolManager<IProvidedNamespace, RdProvidedNamespace> myProvidedNamespacesManger;

    public TypeProvidersManager(IOutOfProcessProtocolManager<IProvidedNamespace, RdProvidedNamespace> providedNamespacesManger)
    {
      myProvidedNamespacesManger = providedNamespacesManger;
    }

    public RdTypeProvider Register(ITypeProvider providedType)
    {
      var tpProtocolModel = new RdTypeProvider();
      
      //tpProtocolModel.GetGeneratedAssemblyContents.Set((lifetime, ) => GetGeneratedAssemblyContents(lifetime, typeProvider));
      tpProtocolModel.GetNamespaces.Set((lifetime, _) => GetTypeProviderNamespaces(lifetime, providedType));
      //tpProtocolModel.ApplyStaticArguments.Set(ApplyStaticArguments);
      //tpProtocolModel.GetStaticParameters.Set(GetStaticParameters);

      return tpProtocolModel;
    }

    private RdTask<RdProvidedNamespace[]> GetTypeProviderNamespaces(Lifetime lifetime, ITypeProvider typeProvider)
    {
      var namespaces = typeProvider.GetNamespaces().Select(myProvidedNamespacesManger.Register).ToArray();
      return RdTask<RdProvidedNamespace[]>.Successful(namespaces);
    }
  }
}
