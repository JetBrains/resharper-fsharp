using System;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
{
  public class ProxyProvidedNamespaceWithCache : IProxyProvidedNamespace
  {
    private readonly IProxyProvidedNamespace myProvidedNamespace;

    public ProxyProvidedNamespaceWithCache(IProxyProvidedNamespace providedNamespace)
    {
      myProvidedNamespace = providedNamespace;

      myNestedNamespaces = new Lazy<IProvidedNamespace[]>(() => myProvidedNamespace.GetNestedNamespaces());
      myRdTypes = new Lazy<RdProvidedType[]>(() => myProvidedNamespace.GetRdTypes());
    }

    public string NamespaceName => myProvidedNamespace.NamespaceName;

    public IProvidedNamespace[] GetNestedNamespaces() => myNestedNamespaces.Value;

    public RdProvidedType[] GetRdTypes() => myRdTypes.Value;

    public RdProvidedType ResolveRdTypeName(string typeName)
    {
      var types = GetRdTypes();
      return types.First(t => t.FullName == typeName);
    }

    public Type[] GetTypes()
    {
      throw new NotImplementedException();
    }

    public Type ResolveTypeName(string typeName)
    {
      throw new NotImplementedException();
    }

    private readonly Lazy<IProvidedNamespace[]> myNestedNamespaces;
    private readonly Lazy<RdProvidedType[]> myRdTypes;
  }
}
