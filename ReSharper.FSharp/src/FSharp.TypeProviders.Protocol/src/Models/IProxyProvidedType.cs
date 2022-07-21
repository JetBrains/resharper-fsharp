using JetBrains.Metadata.Reader.API;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public interface IProxyProvidedType : IRdProvidedEntity
  {
    bool IsCreatedByProvider { get; }
    IClrTypeName GetClrName();
    IProxyTypeProvider TypeProvider { get; }
  }
}
