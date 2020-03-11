using System;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedNamespacesHostFactory : IOutOfProcessHostFactory<RdProvidedNamespaceProcessModel>
  {
    private readonly IReadProvidedCache<Tuple<IProvidedNamespace, int>> myProvidedNamespacesCache;
    private readonly IProvidedRdModelsCreator<IProvidedNamespace, RdProvidedNamespace> myProvidedNamespacesCreator;
    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedTypesCreator;

    public ProvidedNamespacesHostFactory(IReadProvidedCache<Tuple<IProvidedNamespace, int>> providedNamespacesCache,
      IProvidedRdModelsCreator<IProvidedNamespace, RdProvidedNamespace> providedNamespacesCreator,
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypesCreator)
    {
      myProvidedNamespacesCache = providedNamespacesCache;
      myProvidedNamespacesCreator = providedNamespacesCreator;
      myProvidedTypesCreator = providedTypesCreator;
    }

    public RdProvidedNamespaceProcessModel CreateProcessModel()
    {
      var processModel = new RdProvidedNamespaceProcessModel();
      processModel.GetNestedNamespaces.Set(GetNestedNamespaces);
      processModel.GetTypes.Set(GetTypes);
      processModel.ResolveTypeName.Set(ResolveTypeName);
      return processModel;
    }

    private RdTask<int> ResolveTypeName(Lifetime lifetime, ResolveTypeNameArgs args)
    {
      var (providedNamespace, providerId) = myProvidedNamespacesCache.Get(args.Id);

      var providedType = ProvidedType.CreateNoContext(providedNamespace.ResolveTypeName(args.TypeFullName));
      // ReSharper disable once PossibleNullReferenceException
      var rdProvidedTypeId = myProvidedTypesCreator.CreateRdModel(providedType, providerId).EntityId;
      return RdTask<int>.Successful(rdProvidedTypeId);
    }

    private RdTask<int[]> GetTypes(Lifetime lifetime, int entityId)
    {
      var (providedNamespace, providerId) = myProvidedNamespacesCache.Get(entityId);

      var typeIds = providedNamespace
        .GetTypes()
        .Select(ProvidedType.CreateNoContext)
        // ReSharper disable once PossibleNullReferenceException
        .Select(t => myProvidedTypesCreator.CreateRdModel(t, providerId).EntityId)
        .ToArray();
      return RdTask<int[]>.Successful(typeIds);
    }

    private RdTask<RdProvidedNamespace[]> GetNestedNamespaces(Lifetime lifetime, int entityId)
    {
      var (providedNamespace, providerId) = myProvidedNamespacesCache.Get(entityId);

      var namespaces = providedNamespace
        .GetNestedNamespaces()
        .Select(t => myProvidedNamespacesCreator.CreateRdModel(t, providerId))
        .ToArray();
      return RdTask<RdProvidedNamespace[]>.Successful(namespaces);
    }
  }
}
