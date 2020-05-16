using System;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedNamespaceWithCache : IProxyProvidedNamespace
  {
    private readonly RdProvidedNamespace myProvidedNamespace;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;

    private RdProvidedNamespaceProcessModel RdProvidedNamespaceProcessModel =>
      myProcessModel.RdProvidedNamespaceProcessModel;

    private int EntityId => myProvidedNamespace.EntityId;

    public ProxyProvidedNamespaceWithCache(RdProvidedNamespace providedNamespace,
      RdFSharpTypeProvidersLoaderModel processModel,
      ITypeProviderCache cache)
    {
      myProvidedNamespace = providedNamespace;
      myProcessModel = processModel;

      // ReSharper disable once CoVariantArrayConversion
      myNestedNamespaces = new Lazy<IProvidedNamespace[]>(() => RdProvidedNamespaceProcessModel.GetNestedNamespaces
        .Sync(EntityId)
        .Select(t => new ProxyProvidedNamespaceWithCache(t, myProcessModel, cache))
        .ToArray());

      myProvidedTypes = new Lazy<ProvidedType[]>(() => RdProvidedNamespaceProcessModel.GetTypes
        .Sync(EntityId)
        .Select(t => cache.GetOrCreateWithContext(t, ProvidedTypeContext.Empty))
        .ToArray());
    }

    public string NamespaceName => myProvidedNamespace.Name;

    public IProvidedNamespace[] GetNestedNamespaces() => myNestedNamespaces.Value;

    public Type[] GetTypes() =>
      throw new Exception("GetTypes should be unreachable");

    public Type ResolveTypeName(string typeName) =>
      throw new Exception("ResolveTypeName should be unreachable");

    public ProvidedType[] GetProvidedTypes() => myProvidedTypes.Value;

    public ProvidedType ResolveProvidedTypeName(string typeName) =>
      myProvidedTypes.Value.FirstOrDefault(t => t.Name == typeName);

    private readonly Lazy<IProvidedNamespace[]> myNestedNamespaces;
    private readonly Lazy<ProvidedType[]> myProvidedTypes;
  }
}
