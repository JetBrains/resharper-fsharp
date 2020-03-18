using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using JetBrains.Util;
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

      //TODO: подумать о том, что дешевле -- постоянные аллокации ToArray или простоянно хранить массив типов
      myProvidedTypes = new Lazy<ProvidedType[]>(() => RdProvidedNamespaceProcessModel.GetTypes
        .Sync(EntityId)
        .Select(t => cache.GetOrCreateWithContext(t, ProvidedTypeContext.Empty))
        .ToArray());
    }

    public string NamespaceName => myProvidedNamespace.NamespaceName;

    public IProvidedNamespace[] GetNestedNamespaces() => myNestedNamespaces.Value;

    public Type[] GetTypes() =>
      throw new Exception("GetTypes should be unreachable");

    public ProvidedType[] GetProvidedTypes() => myProvidedTypes.Value;

    public Type ResolveTypeName(string typeName) =>
      throw new Exception("ResolveTypeName should be unreachable");

    public ProvidedType ResolveProvidedTypeName(string typeName) =>
      /*
      myCache.GetOrCreateWithContext(
        RdProvidedNamespaceProcessModel.ResolveTypeName.Sync(new ResolveTypeNameArgs(EntityId, typeName)),
        ProvidedTypeContext.Empty);*/
      myProvidedTypes.Value.FirstOrDefault(t => t.FullName == typeName);

    private readonly Lazy<IProvidedNamespace[]> myNestedNamespaces;
    private readonly Lazy<ProvidedType[]> myProvidedTypes;
  }
}
