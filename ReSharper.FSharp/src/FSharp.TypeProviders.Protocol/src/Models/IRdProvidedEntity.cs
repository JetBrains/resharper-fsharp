using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public interface IRdProvidedEntity : IRdProvidedCustomAttributesOwner
  {
    int EntityId { get; }
    RdProvidedEntityType EntityType { get; }
  }

  public interface IRdProvidedCustomAttributesOwner
  {
    RdCustomAttributeData[] Attributes { get; }
  }
}
