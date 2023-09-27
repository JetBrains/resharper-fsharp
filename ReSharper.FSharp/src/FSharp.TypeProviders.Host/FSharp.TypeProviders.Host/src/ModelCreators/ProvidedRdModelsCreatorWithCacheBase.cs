using System.Threading;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public abstract class ProvidedRdModelsCreatorWithCacheBase<T, TU> : IProvidedRdModelsCreatorWithCache<T, TU, int>
    where TU : class
  {
    private readonly IProvidedCache<(T, int), int> myCache;
    private int myCurrentId;
    private readonly Thread myMainThread;

    protected ProvidedRdModelsCreatorWithCacheBase(IProvidedCache<(T, int), int> cache)
    {
      myCache = cache;
      myMainThread = Thread.CurrentThread;
    }

    [ContractAnnotation("providedModel:null => null")]
    public virtual TU CreateRdModel(T providedModel, int typeProviderId)
    {
      if (providedModel == null) return null;

      var id = CreateEntityKey(providedModel);
      var model = CreateRdModelInternal(providedModel, id, typeProviderId);

      myCache.Add(id, (providedModel, typeProviderId));

      return model;
    }

    public virtual int GetOrCreateId(T providedModel, int typeProviderId)
    {
      if (providedModel == null) return ProvidedConst.DefaultId;

      var id = CreateEntityKey(providedModel);

      myCache.Add(id, (providedModel, typeProviderId));
      return id;
    }

    private int CreateEntityKey(T _)
    {
      Assertion.AssertCurrentThread(myMainThread);
      return ++myCurrentId;
    }

    protected abstract TU CreateRdModelInternal(T providedModel, int entityId, int typeProviderId);
  }
}
