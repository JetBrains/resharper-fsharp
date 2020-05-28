using System;
using System.Collections.Generic;
using Microsoft.FSharp.Core.CompilerServices;
using NuGet;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache
{
  public abstract class ProvidedCacheBase<T> : IProvidedCache<T>
  {
    protected readonly IDictionary<int, T> ProvidedModelsCache;

    protected ProvidedCacheBase()
    {
      ProvidedModelsCache = new Dictionary<int, T>();
    }

    public void Add(int id, T value) => ProvidedModelsCache.Add(id, value);

    //Used only for type providers invalidating 
    public abstract void RemoveAll(int typeProviderId);

    public T Get(int key) => ProvidedModelsCache[key];
    public bool Contains(int key) => ProvidedModelsCache.ContainsKey(key);
  }

  public class SimpleProvidedCache<T> : ProvidedCacheBase<Tuple<T, int>>
  {
    public override void RemoveAll(int typeProviderId) =>
      ProvidedModelsCache.RemoveAll(t => t.Value.Item2 == typeProviderId);
  }

  public class TypeProviderCache : ProvidedCacheBase<ITypeProvider>
  {
    public override void RemoveAll(int typeProviderId) =>
      ProvidedModelsCache.RemoveAll(t => t.Key == typeProviderId);
  }

  public class ProvidedEntitiesWithRdModelsCache<T, TU> : ProvidedCacheBase<Tuple<T, TU, int>>
  {
    public override void RemoveAll(int typeProviderId) =>
      ProvidedModelsCache.RemoveAll(t => t.Value.Item3 == typeProviderId);
  }
}
