using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public interface IProxyProvidedNamespace : IProvidedNamespace
  {
    ProvidedType[] GetProvidedTypes();
    ProvidedType ResolveProvidedTypeName(string typeName);
  }
}
