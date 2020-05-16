using System;
using System.Linq;
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

    public void Initialize(RdProvidedNamespaceProcessModel processModel)
    {
      processModel.GetNestedNamespaces.Set(GetNestedNamespaces);
      processModel.GetTypes.Set(GetTypes);
      processModel.ResolveTypeName.Set(ResolveTypeName);
    }

    private int ResolveTypeName(ResolveTypeNameArgs args)
    {
      var (providedNamespace, providerId) = myProvidedNamespacesCache.Get(args.Id);
      var providedType = ProvidedType.CreateNoContext(providedNamespace.ResolveTypeName(args.TypeFullName));
      return myProvidedTypesCreator.CreateRdModel(providedType, providerId).EntityId;
    }

    private int[] GetTypes(int entityId)
    {
      var (providedNamespace, providerId) = myProvidedNamespacesCache.Get(entityId);
      return providedNamespace
        .GetTypes()
        .Select(ProvidedType.CreateNoContext) //TODO: make CreateArray public
        .CreateRdModelsAndReturnIds(myProvidedTypesCreator, providerId);
    }

    private RdProvidedNamespace[] GetNestedNamespaces(int entityId)
    {
      var (providedNamespace, providerId) = myProvidedNamespacesCache.Get(entityId);
      return providedNamespace
        .GetNestedNamespaces()
        .CreateRdModels(myProvidedNamespacesCreator, providerId);
    }
  }
}
