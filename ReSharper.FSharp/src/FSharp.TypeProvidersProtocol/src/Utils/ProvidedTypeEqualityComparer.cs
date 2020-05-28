using System.Collections.Generic;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public class ProvidedTypeEqualityComparer : IEqualityComparer<ProvidedType>
  {
    private (string, string) Key(ProvidedType x) => (x?.Assembly.FullName, x?.FullName);

    public bool Equals(ProvidedType x, ProvidedType y) => Key(x).Equals(Key(y));

    public int GetHashCode(ProvidedType obj)
    {
      return obj.FullName.GetHashCode();
    }
  }
}
