using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Util.dataStructures;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Cache
{
  public class TypeProvidersCache
  {
    private readonly BidirectionalMapOnDictionary<int, (ITypeProvider typeProvider, string envKey)> myTypeProviders =
      new BidirectionalMapOnDictionary<int, (ITypeProvider, string)>(EqualityComparer<int>.Default,
        new TypeProviderComparer());

    private readonly HashSet<int> myInvalidatedProviders = new HashSet<int>();

    public ITypeProvider Get(int typeProviderId) => myTypeProviders[typeProviderId].typeProvider;

    public void Add(int id, (ITypeProvider typeProvider, string) value)
    {
      myTypeProviders.Add(id, value);
      value.typeProvider.Invalidate += (s, obj) => myInvalidatedProviders.Add(id);
    }

    public bool TryGetInfo(ITypeProvider model, string envKey, out (int key, bool isInvalidated) info)
    {
      info = (ProvidedConst.DefaultId, false);

      if (!myTypeProviders.TryGetLeftByRight((model, envKey), out var id))
        return false;

      info = (id, myInvalidatedProviders.Contains(id));
      return id != ProvidedConst.DefaultId;
    }

    public void Remove(int typeProviderId)
    {
      if (!myTypeProviders.TryGetRightByLeft(typeProviderId, out var typeProviderData)) return;
      var (typeProvider, _) = typeProviderData;

      typeProvider.Dispose();
      myTypeProviders.RemoveMapping(typeProviderId, typeProviderData);
      myInvalidatedProviders.Remove(typeProviderId);
    }

    public bool Contains(int typeProviderId) => myTypeProviders.ContainsLeft(typeProviderId);

    public string Dump() =>
      "Type Providers:\n" + string.Join("\n",
        myTypeProviders.Select(t =>
          $"{t.Key} {t.Value.typeProvider.GetType().FullName} (from {Path.GetFileName(t.Value.envKey)})"));
  }
}
