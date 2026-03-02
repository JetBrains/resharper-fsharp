using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Cache;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Utils;

internal static class TestUtils
{
  internal static string GetDumpName(this ITypeProvider typeProvider) =>
    typeProvider.GetType().AssemblyQualifiedName;

  internal static string DumpNames(this TypeProvidersCache tpCache, IEnumerable<int> ids) =>
    string.Join(", ", ids.Select(x => tpCache.Get(x).GetType().FullName).OrderBy());
}
