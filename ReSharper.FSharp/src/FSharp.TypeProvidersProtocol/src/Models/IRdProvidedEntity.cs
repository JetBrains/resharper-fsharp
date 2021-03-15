using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public interface IRdProvidedEntity
  {
    int EntityId { get; }
    RdProvidedEntityType EntityType { get; }
  }
}
