using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using JetBrains.Util.Concurrency;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
  public class ProxyProvidedNamespace : IProxyProvidedNamespace
  {
    private readonly RdProvidedNamespace myProvidedNamespace;

    public ProxyProvidedNamespace(RdProvidedNamespace providedNamespace, IProxyTypeProvider typeProvider,
      TypeProvidersContext typeProvidersContext)
    {
      myProvidedNamespace = providedNamespace;

      myNestedNamespaces = new InterruptibleLazy<ProxyProvidedNamespace[]>(() =>
        providedNamespace.NestedNamespaces
          .Select(t => new ProxyProvidedNamespace(t, typeProvider, typeProvidersContext))
          .ToArray());

      myProvidedTypes =
        new InterruptibleLazy<ProvidedType[]>(() =>
        {
          var types =
            typeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(providedNamespace.Types, typeProvider);

          typeProvider.IsGenerative = types.Any(t => !t.IsErased);

          return types;
        });
    }

    public string NamespaceName => myProvidedNamespace.Name;

    public IProvidedNamespace[] GetNestedNamespaces() => myNestedNamespaces.Value;

    public Type[] GetTypes() =>
      throw new InvalidOperationException("GetTypes should be unreachable");

    public Type ResolveTypeName(string typeName) =>
      throw new InvalidOperationException("ResolveTypeName should be unreachable");

    public ProvidedType[] GetProvidedTypes() => myProvidedTypes.Value;

    public ProvidedType ResolveProvidedTypeName(string typeName)
    {
      foreach (var type in myProvidedTypes.Value)
        if (type.Name == typeName)
          return type;

      return null;
    }

    private readonly InterruptibleLazy<ProxyProvidedNamespace[]> myNestedNamespaces;
    private readonly InterruptibleLazy<ProvidedType[]> myProvidedTypes;
  }
}
