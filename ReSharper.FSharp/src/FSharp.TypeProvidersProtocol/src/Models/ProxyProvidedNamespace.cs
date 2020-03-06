using System;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedNamespace : IProxyProvidedNamespace
  {
    private readonly RdProvidedNamespace myProvidedNamespace;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ITypeProviderCache myCache;

    private RdProvidedNamespaceProcessModel RdProvidedNamespaceProcessModel =>
      myProcessModel.RdProvidedNamespaceProcessModel;

    private int EntityId => myProvidedNamespace.EntityId;

    public ProxyProvidedNamespace(RdProvidedNamespace providedNamespace, RdFSharpTypeProvidersLoaderModel processModel,
      ITypeProviderCache cache)
    {
      myProvidedNamespace = providedNamespace;
      myProcessModel = processModel;
      myCache = cache;
    }

    public string NamespaceName => myProvidedNamespace.NamespaceName;

    public IProvidedNamespace[] GetNestedNamespaces() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedNamespaceProcessModel.GetNestedNamespaces
        .Sync(EntityId)
        .Select(t => new ProxyProvidedNamespace(t, myProcessModel, myCache))
        .ToArray();

    public Type[] GetTypes() =>
      throw new Exception("GetTypes should be unreachable");

    public ProvidedType[] GetProvidedTypes() =>
      RdProvidedNamespaceProcessModel.GetTypes
        .Sync(EntityId)
        .Select(t => myCache.GetOrCreateWithContextProvidedType(t, ProvidedTypeContext.Empty))
        .ToArray();

    public Type ResolveTypeName(string typeName) =>
      throw new Exception("ResolveTypeName should be unreachable");

    public ProvidedType ResolveProvidedTypeName(string typeName) =>
      myCache.GetOrCreateWithContextProvidedType(
        RdProvidedNamespaceProcessModel.ResolveTypeName.Sync(new ResolveTypeNameArgs(EntityId, typeName)),
        ProvidedTypeContext.Empty);
  }
}
