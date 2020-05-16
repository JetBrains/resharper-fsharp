using System;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class TypeProviderRdModelsCreator : IProvidedRdModelsCreator<ITypeProvider, RdTypeProvider>
  {
    private readonly IWriteProvidedCache<ITypeProvider> myCache;
    private int myCurrentId;

    public TypeProviderRdModelsCreator(IWriteProvidedCache<ITypeProvider> cache)
    {
      myCache = cache;
    }

    [ContractAnnotation("providedModel:null => null")]
    public RdTypeProvider CreateRdModel(ITypeProvider providedModel, int typeProviderId)
    {
      if (providedModel == null) return null;

      var id = CreateEntityKey(providedModel);
      var typeProviderType = providedModel.GetType();
      var model = new RdTypeProvider(typeProviderType?.FullName ?? typeProviderType.Name, typeProviderType.Name, id);

      myCache.Add(id, providedModel);
      return model;
    }

    protected int CreateEntityKey(ITypeProvider providedNativeModel)
    {
      Interlocked.Increment(ref myCurrentId);
      return myCurrentId;
    }
  }
}
