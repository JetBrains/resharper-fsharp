using System.Collections.Generic;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public class ProvidedNamespaceEqualityComparer : IEqualityComparer<IProvidedNamespace>
  {
    public bool Equals(IProvidedNamespace x, IProvidedNamespace y)
    {
      return x.NamespaceName == y.NamespaceName;
    }

    public int GetHashCode(IProvidedNamespace obj)
    {
      return obj.NamespaceName.GetHashCode();
    }
  }
}
