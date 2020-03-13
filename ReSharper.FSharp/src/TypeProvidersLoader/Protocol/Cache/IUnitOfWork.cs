using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache
{
  public interface IUnitOfWork
  {
    IOutOfProcessHostFactory<RdFSharpTypeProvidersLoaderModel> TypeProvidersLoaderHostFactory { get; }
    IOutOfProcessHostFactory<RdTypeProviderProcessModel> TypeProvidersHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedNamespaceProcessModel> ProvidedNamespacesHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedTypeProcessModel> ProvidedTypesHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedPropertyInfoProcessModel> ProvidedPropertyInfosHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedMethodInfoProcessModel> ProvidedMethodInfosHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedParameterInfoProcessModel> ProvidedParameterInfosHostFactory { get; }
  }

  public class UnitOfWork: IUnitOfWork
  {
    public IOutOfProcessHostFactory<RdFSharpTypeProvidersLoaderModel> TypeProvidersLoaderHostFactory { get; }
    public IOutOfProcessHostFactory<RdTypeProviderProcessModel> TypeProvidersHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedNamespaceProcessModel> ProvidedNamespacesHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedTypeProcessModel> ProvidedTypesHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedPropertyInfoProcessModel> ProvidedPropertyInfosHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedMethodInfoProcessModel> ProvidedMethodInfosHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedParameterInfoProcessModel> ProvidedParameterInfosHostFactory { get; }
  }
}
