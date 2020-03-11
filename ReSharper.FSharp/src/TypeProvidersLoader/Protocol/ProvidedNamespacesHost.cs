using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedNamespacesHost : OutOfProcessProtocolHostBase<IProvidedNamespace, RdProvidedNamespace>
  {
    private readonly RdProvidedNamespaceProcessModel myProcessModel;
    private readonly IOutOfProcessProtocolHost<ProvidedType, RdProvidedType> myProvidedTypesHost;

    private readonly RdProvidedNamespaceProcessModel myRdProvidedNamespaceProcessModel;

    public ProvidedNamespacesHost(ITypeProvider typeProvider, RdProvidedNamespaceProcessModel processModel) : base(
      new ProvidedNamespaceEqualityComparer())
    {
      myProcessModel = processModel;
      myProvidedTypesHost = new ProvidedTypesHost(typeProvider);

      myRdProvidedNamespaceProcessModel = new RdProvidedNamespaceProcessModel();
      myRdProvidedNamespaceProcessModel.GetNestedNamespaces.Set(GetNestedNamespaces);
      myRdProvidedNamespaceProcessModel.GetTypes.Set(GetTypes);
      myRdProvidedNamespaceProcessModel.ResolveTypeName.Set(ResolveTypeName);
    }

    protected override RdProvidedNamespace CreateRdModel(IProvidedNamespace providedNativeModel, int entityId) =>
      new RdProvidedNamespace(providedNativeModel.NamespaceName, entityId);

    private RdTask<int> ResolveTypeName(Lifetime lifetime, ResolveTypeNameArgs args)
    {
      var providedNamespace = GetEntity(args.Id);

      var providedType = ProvidedType.CreateNoContext(providedNamespace.ResolveTypeName(args.TypeFullName));
      // ReSharper disable once PossibleNullReferenceException
      var rdProvidedTypeId = myProvidedTypesHost.GetRdModel(providedType).EntityId;
      return RdTask<int>.Successful(rdProvidedTypeId);
    }

    private RdTask<int[]> GetTypes(Lifetime lifetime, int entityId)
    {
      var providedNamespace = GetEntity(entityId);

      var typeIds = providedNamespace
        .GetTypes()
        .Select(ProvidedType.CreateNoContext)
        // ReSharper disable once PossibleNullReferenceException
        .Select(t => myProvidedTypesHost.GetRdModel(t).EntityId)
        .ToArray();
      return RdTask<int[]>.Successful(typeIds);
    }

    private RdTask<RdProvidedNamespace[]> GetNestedNamespaces(Lifetime lifetime, int entityId)
    {
      var providedNamespace = GetEntity(entityId);

      var namespaces = providedNamespace
        .GetNestedNamespaces()
        .Select(GetRdModel)
        .ToArray();
      return RdTask<RdProvidedNamespace[]>.Successful(namespaces);
    }
  }
}
