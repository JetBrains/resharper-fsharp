using System.Collections.Generic;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils
{
  public class TypeProviderComparer : IEqualityComparer<(ITypeProvider tp, string envKey)>
  {
    private static string GetName(ITypeProvider tp) => tp?.GetType().FullName;

    public bool Equals((ITypeProvider tp, string envKey) x, (ITypeProvider tp, string envKey) y) =>
      GetName(x.tp) == GetName(y.tp) && x.envKey == y.envKey;

    public int GetHashCode((ITypeProvider tp, string envKey) obj) => GetName(obj.tp).GetHashCode();
  }
}
