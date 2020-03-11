using System;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class TypeProvidersHostFactory : IOutOfProcessHostFactory<RdTypeProviderProcessModel>
  {
    private readonly IReadProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> myProvidedTypesCache;
    private readonly IReadProvidedCache<ITypeProvider> myTypeProvidersCache;
    private readonly IProvidedRdModelsCreator<IProvidedNamespace, RdProvidedNamespace> myProvidedNamespacesCreator;

    public TypeProvidersHostFactory(IReadProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> providedTypesCache,
      IReadProvidedCache<ITypeProvider> typeProvidersCache,
      IProvidedRdModelsCreator<IProvidedNamespace, RdProvidedNamespace> providedNamespacesCreator)
    {
      myProvidedTypesCache = providedTypesCache;
      myTypeProvidersCache = typeProvidersCache;
      myProvidedNamespacesCreator = providedNamespacesCreator;
    }

    public RdTypeProviderProcessModel CreateProcessModel()
    {
      var processModel = new RdTypeProviderProcessModel();
      processModel.GetNamespaces.Set(GetTypeProviderNamespaces);
      processModel.GetProvidedType.Set(GetProvidedType);
      return processModel;
    }

    private RdTask<RdProvidedType> GetProvidedType(Lifetime lifetime, GetProvidedTypeArgs args)
    {
      var (_, type, _) = myProvidedTypesCache.Get(args.Id);
      return RdTask<RdProvidedType>.Successful(type);
    }

    private RdTask<RdProvidedNamespace[]> GetTypeProviderNamespaces(Lifetime lifetime, int providerId)
    {
      var typeProvider = myTypeProvidersCache.Get(providerId);

      var namespaces = typeProvider
        .GetNamespaces()
        .Select(t => myProvidedNamespacesCreator.CreateRdModel(t, providerId))
        .ToArray();
      return RdTask<RdProvidedNamespace[]>.Successful(namespaces);
    }
  }
}
