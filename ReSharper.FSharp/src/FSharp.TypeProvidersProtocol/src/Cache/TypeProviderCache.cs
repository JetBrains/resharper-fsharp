using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
{
  public interface ITypeProviderCache
  {
    [ContractAnnotation("key:null => null")]
    ProvidedType GetOrCreateWithContext(int? key, ProvidedTypeContext context);
  }

  public class TypeProviderCache : ITypeProviderCache
  {
    private readonly RdTypeProvider myProvider;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private RdTypeProviderProcessModel TypeProviderProcessModel => myProcessModel.RdTypeProviderProcessModel;
    private readonly IDictionary<int, ProvidedType> myProvidedTypes;

    public TypeProviderCache(RdTypeProvider provider, RdFSharpTypeProvidersLoaderModel processModel)
    {
      myProvider = provider;
      myProcessModel = processModel;
      myProvidedTypes = new Dictionary<int, ProvidedType>();
    }

    public ProvidedType GetOrCreateWithContext(int? key, ProvidedTypeContext context)
    {
      if (!key.HasValue) return null;
      if (myProvidedTypes.TryGetValue(key.Value, out var providedType)) return providedType;

      providedType = ProxyProvidedType
        .Create(
          TypeProviderProcessModel.GetProvidedType.Sync(new GetProvidedTypeArgs(key.Value)),
          myProcessModel, context, this);
      myProvidedTypes.Add(key.Value, providedType);

      return providedType;
    }
  }
}
