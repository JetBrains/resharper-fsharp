using System.Collections.Generic;
using FSharp.Compiler;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public class ProvidedParameterInfoEqualityComparer : IEqualityComparer<ExtensionTyping.ProvidedParameterInfo>
  {  
    public bool Equals(ExtensionTyping.ProvidedParameterInfo x, ExtensionTyping.ProvidedParameterInfo y)
    {
      return ReferenceEquals(x, y);
    }

    public int GetHashCode(ExtensionTyping.ProvidedParameterInfo obj)
    {
      return obj.Name.GetHashCode();
    }
  }
}
