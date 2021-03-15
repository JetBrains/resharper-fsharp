using System.Collections.Generic;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public class ProvidedAssembliesComparer : IEqualityComparer<ProvidedAssembly>
  {
    public bool Equals(ProvidedAssembly x, ProvidedAssembly y) => x?.FullName == y?.FullName;

    public int GetHashCode(ProvidedAssembly obj) => obj.FullName.GetHashCode();
  }
}
