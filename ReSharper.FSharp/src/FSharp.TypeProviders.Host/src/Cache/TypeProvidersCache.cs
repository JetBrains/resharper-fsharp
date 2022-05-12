using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public ITypeProvider Get(int typeProviderId) => myTypeProviders[typeProviderId].typeProvider;

    public void Add(int id, (ITypeProvider typeProvider, string) value) => myTypeProviders.Add(id, value);

    public bool TryGetInfo(ITypeProvider model, string envKey, out int key) =>
      myTypeProviders.TryGetLeftByRight((model, envKey), out key);

    public void Remove(int typeProviderId)
    {
      if (!myTypeProviders.TryGetRightByLeft(typeProviderId, out var typeProviderData)) return;
      var (typeProvider, _) = typeProviderData;

      typeProvider.Dispose();
      myTypeProviders.RemoveMapping(typeProviderId, typeProviderData);
    }

    public string Dump() =>
      "Type Providers:\n" + string.Join("\n",
        myTypeProviders.Select(t =>
          $"{t.Key} {t.Value.typeProvider.GetType().AssemblyQualifiedName} (from {Path.GetFileName(t.Value.envKey)})"));
  }
}
