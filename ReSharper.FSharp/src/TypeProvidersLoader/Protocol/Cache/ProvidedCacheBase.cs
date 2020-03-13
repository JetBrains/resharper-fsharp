using System.Collections.Generic;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache
{
  public class ProvidedCacheBase<T> : IProvidedCache<T>
  {
    private readonly IDictionary<int, T> myProvidedModelsCache; // just base version

    //private ObjectIDGenerator myObjectIdGenerator;

    public ProvidedCacheBase()
    {
      myProvidedModelsCache = new Dictionary<int, T>();
    }

    public void Add(int id, T value) => myProvidedModelsCache.Add(id, value);

    public T Get(int key) => myProvidedModelsCache[key];
    public bool Contains(int key) => myProvidedModelsCache.ContainsKey(key);
  }
}
