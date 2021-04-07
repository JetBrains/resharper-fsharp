using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache
{
  public interface IProvidedEntitiesCache<out T, in TKey> where T : class
  {
    [ContractAnnotation("key:null => null")]
    T GetOrCreate(TKey key, int typeProviderId, ProvidedTypeContextHolder context);

    /// <summary>
    /// Returns a batch of provided entities with taking into account the keys order
    /// </summary>
    T[] GetOrCreateBatch(TKey[] keys, int typeProviderId, ProvidedTypeContextHolder context);

    void Remove(int typeProviderId);

    string Dump();
  }

  public abstract class ProvidedEntitiesCacheBase<T> : IProvidedEntitiesCache<T, int> where T : class
  {
    protected readonly TypeProvidersContext TypeProvidersContext;
    protected readonly IDictionary<int, T> Entities;
    private readonly IDictionary<int, List<int>> myEntitiesPerProvider;

    protected ProvidedEntitiesCacheBase(TypeProvidersContext typeProvidersContext)
    {
      TypeProvidersContext = typeProvidersContext;
      Entities = new Dictionary<int, T>();
      myEntitiesPerProvider = new Dictionary<int, List<int>>();
    }

    public T GetOrCreate(int key, int typeProviderId, ProvidedTypeContextHolder context)
    {
      if (key == ProvidedConst.DefaultId) return null;
      if (Entities.TryGetValue(key, out var providedEntity)) return providedEntity;

      providedEntity = Create(key, typeProviderId, context);
      AttachToTypeProvider(typeProviderId, key, providedEntity);

      return providedEntity;
    }

    public T[] GetOrCreateBatch(int[] keys, int typeProviderId, ProvidedTypeContextHolder context)
    {
      var entities = new T[keys.Length];

      var groups = keys
        .Select((key, i) => (key, i))
        .GroupBy(t => t.key == ProvidedConst.DefaultId || Entities.ContainsKey(t.key));

      foreach (var group in groups)
      {
        if (group.Key)
          foreach (var (key, i) in group)
            entities[i] = GetOrCreate(key, typeProviderId, context);
        else
        {
          var keysToCreate = group.Select(t => t.key).ToArray();
          var ids = group.Select(t => t.i).ToArray();
          var createdEntities = CreateBatch(keysToCreate, typeProviderId, context);

          for (var i = 0; i < keysToCreate.Length; i++)
          {
            var entity = createdEntities[i];
            AttachToTypeProvider(typeProviderId, keysToCreate[i], entity);
            entities[ids[i]] = entity;
          }
        }
      }

      return entities;
    }

    public void Remove(int typeProviderId)
    {
      if (!myEntitiesPerProvider.TryGetValue(typeProviderId, out var entities)) return;

      foreach (var id in entities)
        Entities.Remove(id);

      myEntitiesPerProvider.Remove(typeProviderId);
    }

    private void AttachToTypeProvider(int typeProviderId, int key, T value)
    {
      Entities.Add(key, value);

      if (!myEntitiesPerProvider.TryGetValue(typeProviderId, out var entities))
      {
        entities = new List<int>();
        myEntitiesPerProvider.Add(typeProviderId, entities);
      }

      entities.Add(key);
    }

    protected abstract T Create(int key, int typeProviderId, ProvidedTypeContextHolder context);
    protected abstract T[] CreateBatch(int[] keys, int typeProviderId, ProvidedTypeContextHolder context);
    public abstract string Dump();
  }
}
