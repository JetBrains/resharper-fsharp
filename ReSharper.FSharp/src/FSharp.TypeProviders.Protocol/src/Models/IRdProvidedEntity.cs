using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public interface IRdProvidedEntity
  {
    int EntityId { get; }
    RdProvidedEntityType EntityType { get; }
  }

  internal interface IProxyProvidedWithContext<T>
  {
    public T ApplyContext(ProvidedTypeContext context);
  }
}
