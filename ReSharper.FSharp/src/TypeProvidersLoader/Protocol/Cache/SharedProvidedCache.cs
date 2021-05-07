using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models;
#if NETFRAMEWORK
using JetBrains.Collections;
#endif
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using NuGet;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache
{
  public abstract class SharedProvidedCache<T> : IBiDirectionalProvidedCache<T, int>
  {
    // typeProviderId defines the main holder of the entity
    private readonly Dictionary<int, (T model, int typeProviderId)> myEntities = new Dictionary<int, (T, int)>();
    protected readonly Dictionary<T, (int id, HashSet<int> referencingProviders)> IdsCache;

    protected SharedProvidedCache(IEqualityComparer<T> equalityComparer) =>
      IdsCache = new Dictionary<T, (int, HashSet<int>)>(equalityComparer);

    public (T model, int typeProviderId) Get(int key) => myEntities[key];

    public void Add(int id, (T model, int typeProviderId) value)
    {
      myEntities.Add(id, value);
      IdsCache.Add(value.model, (id, new HashSet<int> {value.typeProviderId}));
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

    public abstract string Dump();
  }

  public class ProvidedTypesCache : SharedProvidedCache<ProvidedType>
  {
    public ProvidedTypesCache(IEqualityComparer<ProvidedType> equalityComparer) : base(equalityComparer)
    {
    }

    public override string Dump() =>
      "Provided Types:\n" + string.Join("\n",
        IdsCache
          .OrderBy(t => t.Key.FullName)
          .Select(t =>
            $"{t.Key.FullName} tps: {string.Join("|", t.Value.referencingProviders.OrderBy().ToArray())} " +
            $"(from {t.Key.Assembly.GetLogName()})"));
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
