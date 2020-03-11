using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public abstract class OutOfProcessProtocolHostBase<T, TU> : IOutOfProcessProtocolHost<T, TU> where TU : class
  {
    private readonly IDictionary<int, T> myProvidedModelsCache; // just base version
    private readonly IDictionary<T, int> myIdsCache; // TODO: rewrite
    private int myCurrentId;

    protected OutOfProcessProtocolHostBase(IEqualityComparer<T> equalityComparer)
    {
      myIdsCache = new Dictionary<T, int>(equalityComparer);
      myProvidedModelsCache = new Dictionary<int, T>();
    }
    
    public TU GetRdModel(T providedNativeModel)
    {
      if (providedNativeModel == null) return null;

      if (!myIdsCache.TryGetValue(providedNativeModel, out var id))
      {
        id = CreateEntityId(providedNativeModel);
        myIdsCache.Add(providedNativeModel, id);
        myProvidedModelsCache.Add(id, providedNativeModel);
      }

      var rdModel = CreateRdModel(providedNativeModel, id);
      return rdModel;
    }

    protected virtual int CreateEntityId(T providedNativeModel)
    {
      var a = new ObjectIDGenerator();
      Interlocked.Increment(ref myCurrentId);
      return myCurrentId;
    }

    protected virtual T GetEntity(int id) => myProvidedModelsCache[id];
    protected abstract TU CreateRdModel(T providedNativeModel, int entityId);
  }
}
