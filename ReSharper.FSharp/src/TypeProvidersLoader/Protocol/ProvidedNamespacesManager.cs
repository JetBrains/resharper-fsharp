using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.Lifetimes;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using ProvidedType = FSharp.Compiler.ExtensionTyping.ProvidedType;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedNamespacesManager : IOutOfProcessProtocolManager<IProvidedNamespace, RdProvidedNamespace>
  {
    private readonly IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> myProvidedTypesManager;

    public ProvidedNamespacesManager(IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> providedTypesManager)
    {
      myProvidedTypesManager = providedTypesManager;
    }

    public RdProvidedNamespace Register(IProvidedNamespace providedMethod)
    {
      var pnProtocolModel = new RdProvidedNamespace(providedMethod.NamespaceName);

      pnProtocolModel.GetNestedNamespaces.Set((lifetime, _) =>
        GetNestedNamespaces(lifetime, providedMethod));
      pnProtocolModel.GetTypes.Set((lifetime, _) => GetTypes(lifetime, providedMethod));
      pnProtocolModel.ResolveTypeName.Set((lifetime, typeName) =>
        ResolveTypeName(lifetime, providedMethod, typeName));

      return pnProtocolModel;
    }

    private RdTask<RdProvidedType> ResolveTypeName(Lifetime lifetime, IProvidedNamespace providedNamespace,
      string typeName)
    {
      var providedType = ProvidedType.CreateNoContext(providedNamespace.ResolveTypeName(typeName));
      var rdProvidedType = myProvidedTypesManager.Register(providedType);
      return RdTask<RdProvidedType>.Successful(rdProvidedType);
    }

    private RdTask<RdProvidedType[]> GetTypes(Lifetime lifetime, IProvidedNamespace providedNamespace)
    {
      var types = providedNamespace.GetTypes()
        .Select(ProvidedType.CreateNoContext)
        .Select(myProvidedTypesManager.Register)
        .ToArray();

      return RdTask<RdProvidedType[]>.Successful(types);
    }

    private RdTask<RdProvidedNamespace[]> GetNestedNamespaces(Lifetime lifetime,
      IProvidedNamespace providedNamespace)
    {
      var namespaces = providedNamespace.GetNestedNamespaces().Select(Register).ToArray();
      return RdTask<RdProvidedNamespace[]>.Successful(namespaces);
    }
  }
}
