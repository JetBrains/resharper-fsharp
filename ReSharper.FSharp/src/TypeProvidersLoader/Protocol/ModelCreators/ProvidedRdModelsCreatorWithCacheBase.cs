using System.Threading;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public abstract class ProvidedRdModelsCreatorWithCacheBase<T, Tu> : IProvidedRdModelsCreatorWithCache<T, Tu, int>
    where Tu : class
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
    public virtual Tu CreateRdModel(T providedModel, int typeProviderId)
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

    protected abstract Tu CreateRdModelInternal(T providedModel, int entityId, int typeProviderId);
  }
}
