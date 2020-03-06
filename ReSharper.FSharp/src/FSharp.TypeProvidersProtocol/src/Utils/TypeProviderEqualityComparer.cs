using System.Collections.Generic;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public class TypeProviderEqualityComparer : IEqualityComparer<ITypeProvider>
  {
    public bool Equals(ITypeProvider x, ITypeProvider y)
    {
      return x?.GetType().FullName == y?.GetType().FullName;
    }

    public int GetHashCode(ITypeProvider obj)
    {
      return obj.GetType().FullName.GetHashCode();
    }
  }
}
