using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Utils;
#if NETFRAMEWORK
using JetBrains.Collections;
#endif
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.dataStructures;
using static FSharp.Compiler.TypeProviders;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Cache
{
  /// Holds metadata entities (types/assemblies) that can be shared between type provider instances.
  public class MetadataEntitiesCache<T> :
    IEnumerable<KeyValuePair<T, (int id, HashSet<int> referencingProviders)>>,
    IBiDirectionalProvidedCache<T, int> where T : class
  {
    // typeProviderId defines the main holder of the entity
    private readonly Dictionary<int, (T type, int typeProviderId)> myEntities = new();

    protected readonly Dictionary<T, (int id, HashSet<int> referencingProviders)> IdsCache;

    internal MetadataEntitiesCache(IEqualityComparer<T> equalityComparer) =>
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

    public virtual string Dump(TypeProvidersCache tpCache) => "";

    public IEnumerator<KeyValuePair<T, (int id, HashSet<int> referencingProviders)>> GetEnumerator() =>
      IdsCache.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }

  /// Cache holding types:
  /// 1. Created by particular type provider instances.
  /// 2. Coming from referenced assemblies metadata and not depending on particular type provider instances.
  ///
  /// Sharing types created by type providers is not allowed, since type provider instantiations
  /// depend on project configuration and may produce different types with the same name.
  public class ProvidedTypesCache : IBiDirectionalProvidedCache<ProvidedType, int>
  {
    private readonly BidirectionalMapOnDictionary<int, (ProvidedType type, int typeProviderId)>
      myCreatedByProviderTypes;

    private readonly MetadataEntitiesCache<ProvidedType> myMetadataEntitiesCache;

    public ProvidedTypesCache(IEqualityComparer<ProvidedType> equalityComparer)
    {
      myMetadataEntitiesCache = new MetadataEntitiesCache<ProvidedType>(equalityComparer);
      myCreatedByProviderTypes = new BidirectionalMapOnDictionary<int, (ProvidedType type, int typeProviderId)>(
        EqualityComparer<int>.Default,
        EqualityComparer.Create<(ProvidedType type, int typeProviderId)>(
          (x, y) => x.typeProviderId == y.typeProviderId && equalityComparer.Equals(x.type, y.type),
          x => equalityComparer.GetHashCode(x.type)));
    }

    private static bool CanBeSharedBetweenProviders(Type providedType)
    {
      if (providedType.IsCreatedByProvider()) return false;

      if (providedType.IsArray || providedType.IsByRef || providedType.IsPointer)
        return CanBeSharedBetweenProviders(providedType.GetElementType());

      // F# Spec: "Provided type and method definitions may not be generic"
      // so there is no need to check providedType.GetGenericTypeDefinition()
      if (providedType.IsGenericType)
        foreach (var type in providedType.GetGenericArguments())
          if (!CanBeSharedBetweenProviders(type))
            return false;

      return true;
    }

    public (ProvidedType model, int typeProviderId) Get(int key) =>
      myCreatedByProviderTypes.TryGetRightByLeft(key, out var result) ? result : myMetadataEntitiesCache.Get(key);

    public void Add(int id, (ProvidedType model, int typeProviderId) value)
    {
      if (CanBeSharedBetweenProviders(value.model.RawSystemType))
        myMetadataEntitiesCache.Add(id, value);

      else myCreatedByProviderTypes.Add(id, value);
    }

    public void Remove(int typeProviderId)
    {
      myMetadataEntitiesCache.Remove(typeProviderId);
      myCreatedByProviderTypes.RemoveAll(t => t.Value.typeProviderId == typeProviderId);
    }

    public bool TryGetKey(ProvidedType model, int requestingTypeProviderId, out int key) =>
      CanBeSharedBetweenProviders(model.RawSystemType)
        ? myMetadataEntitiesCache.TryGetKey(model, requestingTypeProviderId, out key)
        : myCreatedByProviderTypes.TryGetLeftByRight((model, requestingTypeProviderId), out key);

    public string Dump(TypeProvidersCache tpCache) =>
      string.Join("\n\n",
        "Created by provider Types:\n" + string.Join("\n",
          myCreatedByProviderTypes
            .Select(t =>
              $"{t.Value.type.FullName} (from {t.Value.type.Assembly.GetLogName()}), " +
              $"owners: {tpCache.DumpNames([t.Value.typeProviderId])}")
            .OrderBy()),
        "Provided Types:\n" + string.Join("\n",
          myMetadataEntitiesCache
            .Select(t =>
              $"{t.Key.FullName} (from {t.Key.Assembly.GetLogName()}), " +
              $"owners: {tpCache.DumpNames(t.Value.referencingProviders)}")
            .OrderBy()));
  }

  public class ProvidedAssembliesCache : MetadataEntitiesCache<ProvidedAssembly>
  {
    public ProvidedAssembliesCache(IEqualityComparer<ProvidedAssembly> equalityComparer) : base(equalityComparer)
    {
    }

    public override string Dump(TypeProvidersCache tpCache) =>
      "Provided Assemblies:\n" + string.Join("\n",
        IdsCache
          .Select(t => (Assembly: t.Key, Providers: t.Value.referencingProviders.OrderBy().ToArray()))
          .Select(t => t.Assembly.GetLogName() +
                       (t.Assembly.GetName().Name is "mscorlib" or "netstandard"
                         ? ""
                         : $", owners: {tpCache.DumpNames(t.Providers)}"))
          .OrderBy());
  }
}
