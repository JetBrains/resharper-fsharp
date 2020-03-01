using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public interface IProxyProvidedNamespace : IProvidedNamespace
  {
    RdProvidedType[] GetRdTypes();
    RdProvidedType ResolveRdTypeName(string typeName);
  }
}
