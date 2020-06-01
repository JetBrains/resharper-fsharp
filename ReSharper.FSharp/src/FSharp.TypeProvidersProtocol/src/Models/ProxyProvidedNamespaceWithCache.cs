using System;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using JetBrains.Util.Concurrency;
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
      int typeProviderId,
      RdFSharpTypeProvidersLoaderModel processModel,
      IProvidedTypesCache cache)
    {
      myProvidedNamespace = providedNamespace;
      myProcessModel = processModel;

      // ReSharper disable once CoVariantArrayConversion
      myNestedNamespaces = new InterruptibleLazy<IProvidedNamespace[]>(() => RdProvidedNamespaceProcessModel
        .GetNestedNamespaces
        .Sync(EntityId)
        .Select(t => new ProxyProvidedNamespaceWithCache(t, typeProviderId, myProcessModel, cache))
        .ToArray());

      myProvidedTypes = new InterruptibleLazy<ProvidedType[]>(() => RdProvidedNamespaceProcessModel.GetTypes
        .Sync(EntityId)
        .Select(t => cache.GetOrCreateWithContext(t, typeProviderId, ProvidedTypeContext.Empty))
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

    private readonly InterruptibleLazy<IProvidedNamespace[]> myNestedNamespaces;
    private readonly InterruptibleLazy<ProvidedType[]> myProvidedTypes;
  }
}
