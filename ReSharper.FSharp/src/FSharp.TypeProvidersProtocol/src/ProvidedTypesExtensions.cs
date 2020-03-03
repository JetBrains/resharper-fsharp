using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol
{
  public static class ProvidedTypesExtensions
  {
    public static ProvidedType WithCache(this ProvidedType providedType)
    {
      return new ProvidedTypeWithCache(providedType);
    }
    
    public static ProvidedType[] WithCache(this ProvidedType[] providedTypes)
    {
      return providedTypes.Select(t => t.WithCache()).ToArray();
    }
  }
}
