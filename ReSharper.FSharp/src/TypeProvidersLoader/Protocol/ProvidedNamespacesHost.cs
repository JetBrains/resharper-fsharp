using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedNamespacesHost : OutOfProcessProtocolHostBase<IProvidedNamespace, RdProvidedNamespace>
  {
    private readonly IOutOfProcessProtocolHost<ProvidedType, RdProvidedType> myProvidedTypesHost;

    public ProvidedNamespacesHost() : base(new ProvidedNamespaceEqualityComparer())
    {
      myProvidedTypesHost = new ProvidedTypesHost();
    }

    protected override RdProvidedNamespace CreateRdModel(
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
      var rdProvidedType = myProvidedTypesHost.GetRdModel(providedType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(rdProvidedType);
    }

    private RdTask<RdProvidedType[]> GetTypes(
      Lifetime lifetime,
      IProvidedNamespace providedNamespace,
      ITypeProvider providedModelOwner)
    {
      var types = providedNamespace.GetTypes()
        .Select(ProvidedType.CreateNoContext)
        .Select(t => myProvidedTypesHost.GetRdModel(t, providedModelOwner))
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
        .Select(t => GetRdModel(t, providedModelOwner)).ToArray();
      return RdTask<RdProvidedNamespace[]>.Successful(namespaces);
    }
  }
}
