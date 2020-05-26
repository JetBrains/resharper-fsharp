namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public interface IProxyProvidedNamedEntity : IRdProvidedEntity
  {
    public string FullName { get; }
  }
}
