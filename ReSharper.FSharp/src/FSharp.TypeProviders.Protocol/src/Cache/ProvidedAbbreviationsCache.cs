using System;
using System.Collections.Concurrent;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache
{
  public class ProvidedAbbreviationsCache
  {
    private readonly ConcurrentDictionary<IPsiModule, ConcurrentDictionary<string, ProxyProvidedTypeWithContext>>
      myCache = new();

    private readonly ConcurrentQueue<(IPsiModule module, string clrName)> myQueueToInvalidate = new();

    private void Invalidate()
    {
      while (myQueueToInvalidate.TryDequeue(out var itemToInvalidate) &&
             myCache.TryGetValue(itemToInvalidate.module, out var typesGroup) &&
             typesGroup.TryGetValue(itemToInvalidate.clrName, out var type))
      {
        var (generativeTypesToIlTypeRefsMap, _) = type.Context.GetDictionaries();
        foreach (var ilTypeRef in generativeTypesToIlTypeRefsMap.Values)
          typesGroup.TryRemove(ilTypeRef.BasicQualifiedName, out _);
      }
    }

    public ProxyProvidedTypeWithContext this[string clrName]
    {
      set
      {
        Invalidate();

        var module = value.TypeProvider.Module.NotNull();
        if (!myCache.TryGetValue(module, out var typesGroup))
        {
          typesGroup = new ConcurrentDictionary<string, ProxyProvidedTypeWithContext>();
          myCache[module] = typesGroup;
        }

        typesGroup[clrName] = value;
      }
    }

    public bool TryGet(IPsiModule module, IClrTypeName clrName, out ProxyProvidedTypeWithContext providedType)
    {
      providedType = null;
      return myCache.TryGetValue(module, out var typesGroup) &&
             typesGroup.TryGetValue(clrName.FullName, out providedType);
    }

    public void MarkAsInvalidated(IPsiModule module, IClrTypeName clrName) =>
      myQueueToInvalidate.Enqueue((module, clrName.FullName));

    public void Remove(IProxyTypeProvider typeProvider)
    {
      var (module, tpId) = (typeProvider.Module.NotNull(), typeProvider.EntityId);
      if (myCache.TryGetValue(module, out var typesGroup))
        typesGroup.RemoveAll(t => t.Value.EntityId == tpId);
    }
  }
}
