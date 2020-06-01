using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using NuGet;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
{
  public interface IProvidedTypesCache
  {
    [ContractAnnotation("key:null => null")]
    ProvidedType GetOrCreateWithContext(int? key, int typeProviderId, ProvidedTypeContext context);

    void Invalidate(int typeProviderId);
  }

  public class ProvidedTypesCache : IProvidedTypesCache
  {
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private RdTypeProviderProcessModel TypeProviderProcessModel => myProcessModel.RdTypeProviderProcessModel;
    private readonly IDictionary<int, Tuple<ProvidedType, int>> myProvidedTypes;

    public ProvidedTypesCache(RdFSharpTypeProvidersLoaderModel processModel)
    {
      myProcessModel = processModel;
      myProvidedTypes = new Dictionary<int, Tuple<ProvidedType, int>>();
    }

    public ProvidedType GetOrCreateWithContext(int? key, int typeProviderId, ProvidedTypeContext context)
    {
      if (!key.HasValue) return null;
      if (myProvidedTypes.TryGetValue(key.Value, out var providedTypeMeta)) return providedTypeMeta.Item1;

      var providedType = ProxyProvidedTypeWithCache.Create(
        TypeProviderProcessModel.GetProvidedType.Sync(new GetProvidedTypeArgs(key.Value)), typeProviderId,
        myProcessModel, context, this);
      myProvidedTypes.Add(key.Value, new Tuple<ProvidedType, int>(providedType, typeProviderId));

      return providedType;
    }

    public void Invalidate(int typeProviderId)
    {
      myProvidedTypes.RemoveAll(t => t.Value.Item2 == typeProviderId);
    }
  }
}
