using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.Lifetimes;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedNamespacesManager : OutOfProcessProtocolManagerBase<IProvidedNamespace, RdProvidedNamespace>
  {
    private readonly IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> myProvidedTypesManager;

    public ProvidedNamespacesManager()
    {
      myProvidedTypesManager = new ProvidedTypesManager();
    }

    protected override RdProvidedNamespace CreateProcessModel(
      IProvidedNamespace providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var pnProtocolModel = new RdProvidedNamespace(providedNativeModel.NamespaceName);

      pnProtocolModel.GetNestedNamespaces.Set((lifetime, _) =>
        GetNestedNamespaces(lifetime, providedNativeModel, providedModelOwner));
      pnProtocolModel.GetTypes.Set((lifetime, _) => GetTypes(lifetime, providedNativeModel, providedModelOwner));
      pnProtocolModel.ResolveTypeName.Set((lifetime, typeName) =>
        ResolveTypeName(lifetime, providedNativeModel, providedModelOwner, typeName));

      return pnProtocolModel;
    }

    private RdTask<RdProvidedType> ResolveTypeName(
      Lifetime lifetime,
      IProvidedNamespace providedNamespace,
      ITypeProvider providedModelOwner,
      string typeName)
    {
      var providedType = ProvidedType.CreateNoContext(providedNamespace.ResolveTypeName(typeName));
      var rdProvidedType = myProvidedTypesManager.Register(providedType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(rdProvidedType);
    }

    private RdTask<RdProvidedType[]> GetTypes(
      Lifetime lifetime,
      IProvidedNamespace providedNamespace,
      ITypeProvider providedModelOwner)
    {
      var types = providedNamespace.GetTypes()
        .Select(ProvidedType.CreateNoContext)
        .Select(t => myProvidedTypesManager.Register(t, providedModelOwner))
        .ToArray();

      return RdTask<RdProvidedType[]>.Successful(types);
    }

    private RdTask<RdProvidedNamespace[]> GetNestedNamespaces(
      Lifetime lifetime,
      IProvidedNamespace providedNamespace,
      ITypeProvider providedModelOwner)
    {
      var namespaces = providedNamespace
        .GetNestedNamespaces()
        .Select(t => Register(t, providedModelOwner)).ToArray();
      return RdTask<RdProvidedNamespace[]>.Successful(namespaces);
    }
  }
}
