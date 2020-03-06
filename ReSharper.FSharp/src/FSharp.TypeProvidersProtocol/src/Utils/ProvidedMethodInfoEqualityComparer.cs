using System.Collections.Generic;
using FSharp.Compiler;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public class ProvidedMethodInfoEqualityComparer : IEqualityComparer<ExtensionTyping.ProvidedMethodInfo>
  {
    public bool Equals(ExtensionTyping.ProvidedMethodInfo x, ExtensionTyping.ProvidedMethodInfo y)
    {
      return ReferenceEquals(x, y);
    }

    public int GetHashCode(ExtensionTyping.ProvidedMethodInfo obj)
    {
      return obj.Name.GetHashCode();
    }
  }
}
