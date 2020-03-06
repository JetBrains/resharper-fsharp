using System;
using System.Collections.Generic;
using FSharp.Compiler;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public class ProvidedTypeEqualityComparer : IEqualityComparer<ExtensionTyping.ProvidedType>
  {
    public bool Equals(ExtensionTyping.ProvidedType x, ExtensionTyping.ProvidedType y)
    {
      return string.Equals(x.FullName, y.FullName, StringComparison.Ordinal);
    }

    public int GetHashCode(ExtensionTyping.ProvidedType obj)
    {
      return obj.FullName.GetHashCode();
    }
  }
}
