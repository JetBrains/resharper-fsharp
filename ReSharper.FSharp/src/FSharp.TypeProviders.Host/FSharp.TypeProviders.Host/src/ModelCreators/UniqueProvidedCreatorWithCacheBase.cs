using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public abstract class
    UniqueProvidedCreatorWithCacheBase<T, TU> : ProvidedRdModelsCreatorWithCacheBase<T, TU>
    where TU : RdProvidedEntity
  {
    private readonly IBiDirectionalProvidedCache<T, int> myCache;

    protected UniqueProvidedCreatorWithCacheBase(IBiDirectionalProvidedCache<T, int> cache) : base(cache) =>
      myCache = cache;

    [ContractAnnotation("providedModel:null => null")]
    public override TU CreateRdModel(T providedModel, int typeProviderId)
    {
      if (providedModel == null) return null;
      if (myCache.TryGetKey(providedModel, typeProviderId, out var id))
        return CreateRdModelInternal(providedModel, id, typeProviderId);

      var model = base.CreateRdModel(providedModel, typeProviderId);
      return model;
    }

    public override int GetOrCreateId(T providedModel, int typeProviderId)
    {
      if (providedModel == null) return ProvidedConst.DefaultId;
      return myCache.TryGetKey(providedModel, typeProviderId, out var id)
        ? id
        : base.GetOrCreateId(providedModel, typeProviderId);
    }
  }
}
