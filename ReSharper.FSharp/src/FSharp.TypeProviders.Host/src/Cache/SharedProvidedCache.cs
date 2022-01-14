using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if NETFRAMEWORK
using JetBrains.Collections;
#endif
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.dataStructures;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Cache
{
  public class SharedProvidedCache<T> :
    IEnumerable<KeyValuePair<T, (int id, HashSet<int> referencingProviders)>>,
    IBiDirectionalProvidedCache<T, int> where T : class
  {
    // typeProviderId defines the main holder of the entity
    private readonly Dictionary<int, (T type, int typeProviderId)> myEntities =
      new Dictionary<int, (T type, int typeProviderId)>();

    protected readonly Dictionary<T, (int id, HashSet<int> referencingProviders)> IdsCache;

    internal SharedProvidedCache(IEqualityComparer<T> equalityComparer) =>
      IdsCache = new Dictionary<T, (int, HashSet<int>)>(equalityComparer);

    public (T model, int typeProviderId) Get(int key) => myEntities[key];

    public void Add(int id, (T model, int typeProviderId) value)
    {
      myEntities.Add(id, value);
      IdsCache.Add(value.model, (id, new HashSet<int> { value.typeProviderId }));
    }

    // [Not pure] Remembers providers requesting a model
    public bool TryGetKey(T model, int requestingTypeProviderId, out int key)
    {
      key = ProvidedConst.DefaultId;
      if (IdsCache.TryGetValue(model, out var data))
      {
        data.referencingProviders.Add(requestingTypeProviderId);
        key = data.id;
      }

      return key != ProvidedConst.DefaultId;
    }

    public void Remove(int typeProviderId) =>
      IdsCache.RemoveAll(t =>
      {
        var (entity, (id, providers)) = t;
        // Remove the type provider link to the entity
        providers.Remove(typeProviderId);

        if (providers.Any())
        {
          // Update the main entity holder
          myEntities[id] = (entity, providers.First());
          return false;
        }

        myEntities.Remove(id);
        return true;
      });

    public virtual string Dump() => "";

    public IEnumerator<KeyValuePair<T, (int id, HashSet<int> referencingProviders)>> GetEnumerator() =>
      IdsCache.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }

  public class ProvidedTypesCache : IBiDirectionalProvidedCache<ProvidedType, int>
  {
    private readonly BidirectionalMapOnDictionary<int, (ProvidedType type, int typeProviderId)>
      myCreatedByProviderTypes;

    private readonly SharedProvidedCache<ProvidedType> mySharedProvidedCache;

    public ProvidedTypesCache(IEqualityComparer<ProvidedType> equalityComparer)
    {
      mySharedProvidedCache = new SharedProvidedCache<ProvidedType>(equalityComparer);
      myCreatedByProviderTypes = new BidirectionalMapOnDictionary<int, (ProvidedType type, int typeProviderId)>(
        EqualityComparer<int>.Default,
        EqualityComparer.Create<(ProvidedType type, int typeProviderId)>(
          (x, y) => x.typeProviderId == y.typeProviderId && equalityComparer.Equals(x.type, y.type),
          x => equalityComparer.GetHashCode(x.type)));
    }

    public (ProvidedType model, int typeProviderId) Get(int key) =>
      myCreatedByProviderTypes.TryGetRightByLeft(key, out var result) ? result : mySharedProvidedCache.Get(key);

    public void Add(int id, (ProvidedType model, int typeProviderId) value)
    {
      if (value.model.IsCreatedByProvider())
        myCreatedByProviderTypes.Add(id, value);

      else mySharedProvidedCache.Add(id, value);
    }

    public void Remove(int typeProviderId)
    {
      mySharedProvidedCache.Remove(typeProviderId);
      myCreatedByProviderTypes.RemoveAll(t => t.Value.typeProviderId == typeProviderId);
    }

    public bool TryGetKey(ProvidedType model, int requestingTypeProviderId, out int key) =>
      model.IsCreatedByProvider()
        ? myCreatedByProviderTypes.TryGetLeftByRight((model, requestingTypeProviderId), out key)
        : mySharedProvidedCache.TryGetKey(model, requestingTypeProviderId, out key);

    public string Dump() =>
      string.Join("\n\n",
        "Created by provider Types:\n" + string.Join("\n",
          myCreatedByProviderTypes
            .OrderBy(t => t.Value.type.FullName)
            .Select(t =>
              $"{t.Value.type.FullName} tp: {t.Value.typeProviderId} " +
              $"(from {t.Value.type.Assembly.GetLogName()})")),
        "Provided Types:\n" + string.Join("\n",
          mySharedProvidedCache
            .OrderBy(t => t.Key.FullName)
            .Select(t =>
              $"{t.Key.FullName} tps: {string.Join("|", t.Value.referencingProviders.OrderBy().ToArray())} " +
              $"(from {t.Key.Assembly.GetLogName()})")));
  }

  public class ProvidedAssembliesCache : SharedProvidedCache<ProvidedAssembly>
  {
    public ProvidedAssembliesCache(IEqualityComparer<ProvidedAssembly> equalityComparer) : base(equalityComparer)
    {
    }

    public override string Dump() =>
      "Provided Assemblies:\n" + string.Join("\n",
        IdsCache
          .Select(t => (Name: t.Key.GetLogName(), Providers: t.Value.referencingProviders.OrderBy().ToArray()))
          .OrderBy(t => t.Name)
          .Select(t => $"{t.Name} tps: {string.Join("|", t.Providers)}"));
  }
}
