using System.Collections.Generic;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public class ProvidedTypesComparer : IEqualityComparer<ProvidedType>
  {
    private ProvidedTypesComparer()
    {
    }

    public bool Equals(ProvidedType x, ProvidedType y) =>
      x?.FullName == y?.FullName && x?.Assembly.FullName == y?.Assembly.FullName;

    public int GetHashCode(ProvidedType x) => x.FullName.GetHashCode();

    public static readonly ProvidedTypesComparer Instance = new ProvidedTypesComparer();
  }
}
