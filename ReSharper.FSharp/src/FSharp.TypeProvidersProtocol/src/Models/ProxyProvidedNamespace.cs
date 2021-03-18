using System;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using JetBrains.Util.Concurrency;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedNamespace : IProxyProvidedNamespace
  {
    private readonly RdProvidedNamespace myProvidedNamespace;

    public ProxyProvidedNamespace(RdProvidedNamespace providedNamespace, int typeProviderId,
      TypeProvidersContext typeProvidersContext)
    {
      myProvidedNamespace = providedNamespace;
      var context = ProvidedTypeContextHolder.Create();

      myNestedNamespaces = new InterruptibleLazy<ProxyProvidedNamespace[]>(() =>
        providedNamespace.NestedNamespaces
          .Select(t => new ProxyProvidedNamespace(t, typeProviderId, typeProvidersContext))
          .ToArray());

      myProvidedTypes =
        new InterruptibleLazy<ProvidedType[]>(() =>
          typeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(providedNamespace.Types, typeProviderId,
            context));
    }

    public string NamespaceName => myProvidedNamespace.Name;

    // ReSharper disable once CoVariantArrayConversion
    public IProvidedNamespace[] GetNestedNamespaces() => myNestedNamespaces.Value;

    public Type[] GetTypes() =>
      throw new InvalidOperationException("GetTypes should be unreachable");

    public Type ResolveTypeName(string typeName) =>
      throw new InvalidOperationException("ResolveTypeName should be unreachable");

    public ProvidedType[] GetProvidedTypes() => myProvidedTypes.Value;

    public ProvidedType ResolveProvidedTypeName(string typeName) =>
      myProvidedTypes.Value.FirstOrDefault(t => t.Name == typeName);

    private readonly InterruptibleLazy<ProxyProvidedNamespace[]> myNestedNamespaces;
    private readonly InterruptibleLazy<ProvidedType[]> myProvidedTypes;
  }
}
