using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.Threading;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache
{
  public interface IProvidedEntitiesCache<out T, in TKey, in TParam> where T : class
  {
    [ContractAnnotation("key:null => null")]
    T GetOrCreate(TKey key, IProxyTypeProvider typeProvider, TParam parameters = default);

    /// <summary>
    /// Returns a batch of provided entities with taking into account the keys order
    /// </summary>
    T[] GetOrCreateBatch(TKey[] keys, IProxyTypeProvider typeProvider, TParam parameters = default);

    void Remove(int typeProviderId);

    string Dump();
  }

  public abstract class ProvidedEntitiesCacheBase<T, TKey, TParam> : IProvidedEntitiesCache<T, TKey, TParam>
    where T : class
  {
    protected readonly TypeProvidersContext TypeProvidersContext;
    protected readonly ConcurrentDictionary<TKey, T> Entities;
    protected readonly IDictionary<int, List<TKey>> EntitiesPerProvider;
    private readonly object myEntitiesPerProviderLockObj = new();
    private SpinWaitLock myEntitiesLock;

    protected ProvidedEntitiesCacheBase(TypeProvidersContext typeProvidersContext)
    {
      TypeProvidersContext = typeProvidersContext;
      Entities = new ConcurrentDictionary<TKey, T>();
      EntitiesPerProvider = new Dictionary<int, List<TKey>>();
    }

    public T GetOrCreate(TKey key, IProxyTypeProvider typeProvider, TParam parameters = default)
    {
      if (!KeyHasValue(key)) return null;
      if (Entities.TryGetValue(key, out var providedEntity)) return providedEntity;

      try
      {
        myEntitiesLock.Enter();
        if (Entities.TryGetValue(key, out providedEntity)) return providedEntity;
        providedEntity = Create(key, typeProvider, parameters);
        AttachToTypeProvider(typeProvider.EntityId, key, providedEntity);
        return providedEntity;
      }
      finally
      {
        myEntitiesLock.Exit();
      }
    }

    public T[] GetOrCreateBatch(TKey[] keys, IProxyTypeProvider typeProvider, TParam parameters = default)
    {
      if (keys.Length == 1) return new[] { GetOrCreate(keys[0], typeProvider, parameters) };

      var entities = new T[keys.Length];

      var groups = keys
        .Select((key, i) => (key, i))
        .GroupBy(t => !KeyHasValue(t.key) || Entities.ContainsKey(t.key));

      foreach (var group in groups)
      {
        if (group.Key)
          foreach (var (key, i) in group)
          {
            if (!KeyHasValue(key)) continue;
            Entities.TryGetValue(key, out var providedEntity);
            Assertion.AssertNotNull(providedEntity, "Possible concurrent Remove(typeProviderId) call");
            entities[i] = providedEntity;
          }

        else
        {
          var keysToCreate = group.Select(t => t.key).ToArray();
          var ids = group.Select(t => t.i).ToArray();
          var createdEntities = CreateBatch(keysToCreate, typeProvider, parameters);

          for (var i = 0; i < keysToCreate.Length; i++)
          {
            var entity = createdEntities[i];
            AttachToTypeProvider(typeProvider.EntityId, keysToCreate[i], entity);
            entities[ids[i]] = entity;
          }
        }
      }

      return entities;
    }

    public void Remove(int typeProviderId)
    {
      lock (myEntitiesPerProviderLockObj)
      {
        if (!EntitiesPerProvider.TryGetValue(typeProviderId, out var entities)) return;

        foreach (var id in entities)
          Entities.TryRemove(id, out _);

        EntitiesPerProvider.Remove(typeProviderId);
      }
    }

    private void AttachToTypeProvider(int typeProviderId, TKey key, T value)
    {
      if (!Entities.TryAdd(key, value)) return;
      lock (myEntitiesPerProviderLockObj)
      {
        if (!EntitiesPerProvider.TryGetValue(typeProviderId, out var entities))
        {
          entities = new List<TKey>();
          EntitiesPerProvider.Add(typeProviderId, entities);
        }

        entities.Add(key);
      }
    }

    protected abstract bool KeyHasValue(TKey key);
    protected abstract T Create(TKey key, IProxyTypeProvider typeProvider, TParam parameters);
    protected abstract T[] CreateBatch(TKey[] keys, IProxyTypeProvider typeProvider, TParam parameters);
    public abstract string Dump();
  }
}
