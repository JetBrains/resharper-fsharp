using System;
using System.Linq;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using JetBrains.Rd.Tasks;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedNamespacesHostFactory : IOutOfProcessHostFactory<RdProvidedNamespaceProcessModel>
  {
    private readonly UnitOfWork myUnitOfWork;

    public ProvidedNamespacesHostFactory(UnitOfWork unitOfWork)
    {
      myUnitOfWork = unitOfWork;
    }

    public void Initialize(RdProvidedNamespaceProcessModel processModel)
    {
      processModel.GetNestedNamespaces.Set(GetNestedNamespaces);
      processModel.GetTypes.Set(GetTypes);
      processModel.ResolveTypeName.Set(ResolveTypeName);
    }

    private int ResolveTypeName(ResolveTypeNameArgs args)
    {
      var (providedNamespace, providerId) = myUnitOfWork.ProvidedNamespacesCache.Get(args.Id);
      var providedType = ProvidedType.CreateNoContext(providedNamespace.ResolveTypeName(args.TypeFullName));
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedType, providerId).EntityId;
    }

    private int[] GetTypes(int entityId)
    {
      var (providedNamespace, providerId) = myUnitOfWork.ProvidedNamespacesCache.Get(entityId);
      return providedNamespace
        .GetTypes()
        .Select(ProvidedType.CreateNoContext)
        .CreateRdModelsAndReturnIds(myUnitOfWork.ProvidedTypeRdModelsCreator, providerId);
    }

    private RdProvidedNamespace[] GetNestedNamespaces(int entityId)
    {
      var (providedNamespace, providerId) = myUnitOfWork.ProvidedNamespacesCache.Get(entityId);
      return providedNamespace
        .GetNestedNamespaces()
        .CreateRdModels(myUnitOfWork.ProvidedNamespaceRdModelsCreator, providerId);
    }
  }
}
