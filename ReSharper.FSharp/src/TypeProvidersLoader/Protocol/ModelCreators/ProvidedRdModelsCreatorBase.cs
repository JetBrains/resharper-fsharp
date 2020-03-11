using System;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public abstract class ProvidedRdModelsCreatorBase<T, TU> : IProvidedRdModelsCreator<T, TU> where TU : class
  {
    private readonly IWriteProvidedCache<Tuple<T, int>> myCache;
    private int myCurrentId;

    protected ProvidedRdModelsCreatorBase(IWriteProvidedCache<Tuple<T, int>> cache)
    {
      myCache = cache;
    }

    [ContractAnnotation("providedModel:null => null")]
    public TU CreateRdModel(T providedModel, int typeProviderId)
    {
      if (providedModel == null) return null;

      var id = CreateEntityKey(providedModel);
      var model = CreateRdModelInternal(providedModel, id);

      myCache.Add(id, new Tuple<T, int>(providedModel, typeProviderId));

      return model;
    }

    protected int CreateEntityKey(T providedNativeModel)
    {
      Interlocked.Increment(ref myCurrentId);
      return myCurrentId;
    }

    protected abstract TU CreateRdModelInternal(T providedNativeModel, int entityId);
  }
}
