using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Models
{
  public interface IOutOfProcessProxyProvidedNamespace : IProvidedNamespace
  {
    RdProvidedType[] GetRdTypes();
    RdProvidedType ResolveRdTypeName(string typeName);
  }
}
