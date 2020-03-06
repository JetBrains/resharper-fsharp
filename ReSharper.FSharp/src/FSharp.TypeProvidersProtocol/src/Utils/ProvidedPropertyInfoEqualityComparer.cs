using System.Collections.Generic;
using FSharp.Compiler;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public class ProvidedPropertyInfoEqualityComparer : IEqualityComparer<ExtensionTyping.ProvidedPropertyInfo>
  {
    public bool Equals(ExtensionTyping.ProvidedPropertyInfo x, ExtensionTyping.ProvidedPropertyInfo y)
    {
      return x.Name == y.Name;
    }

    public int GetHashCode(ExtensionTyping.ProvidedPropertyInfo obj)
    {
      return obj.Name.GetHashCode();
    }
  }
}
