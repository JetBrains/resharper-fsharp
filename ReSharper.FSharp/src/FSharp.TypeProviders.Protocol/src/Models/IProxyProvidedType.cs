using JetBrains.Metadata.Reader.API;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public interface IProxyProvidedType : IRdProvidedEntity
  {
    bool IsCreatedByProvider { get; }
    IClrTypeName GetClrName();
    string DisplayName { get; }
    IProxyTypeProvider TypeProvider { get; }
  }
}
