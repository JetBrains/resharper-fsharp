using System.Collections.Generic;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache
{
  public class ProvidedCacheBase<T, TU> : IReadProvidedCache<T, TU>, IWriteProvidedCache<T, TU>
  {
    private readonly IDictionary<int, ProvidedExtendedData<T, TU>> myProvidedModelsCache; // just base version

    //private ObjectIDGenerator myObjectIdGenerator;

    protected ProvidedCacheBase()
    {
      myProvidedModelsCache = new Dictionary<int, ProvidedExtendedData<T, TU>>();
    }

    public void Add(int id, ProvidedExtendedData<T, TU> value) => myProvidedModelsCache.Add(id, value);

    public ProvidedExtendedData<T, TU> Get(int key) => myProvidedModelsCache[key];
  }
}
