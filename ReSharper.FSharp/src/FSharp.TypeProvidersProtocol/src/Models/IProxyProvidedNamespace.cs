using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public interface IProxyProvidedNamespace : IProvidedNamespace
  {
    ProvidedType[] GetProvidedTypes();
    ProvidedType ResolveProvidedTypeName(string typeName);
  }
}
