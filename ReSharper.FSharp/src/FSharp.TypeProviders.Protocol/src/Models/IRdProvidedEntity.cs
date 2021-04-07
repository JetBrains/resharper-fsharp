using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public interface IRdProvidedEntity
  {
    int EntityId { get; }
    RdProvidedEntityType EntityType { get; }
  }
}
