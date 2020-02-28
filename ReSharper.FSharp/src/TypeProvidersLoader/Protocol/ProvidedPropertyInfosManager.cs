using FSharp.Compiler;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using ProvidedPropertyInfo = FSharp.Compiler.ExtensionTyping.ProvidedPropertyInfo;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedPropertyInfoManager : IOutOfProcessProtocolManager<ProvidedPropertyInfo, RdProvidedPropertyInfo>
  {
    private readonly IOutOfProcessProtocolManager<ExtensionTyping.ProvidedType, RdProvidedType> myProvidedTypesManager;

    public ProvidedPropertyInfoManager()
    {
      myProvidedTypesManager = new ProvidedTypesManager(this);
    }

    public RdProvidedPropertyInfo Register(ProvidedPropertyInfo providedMethod)
    {
      var ppModel = new RdProvidedPropertyInfo(providedMethod.CanRead,
        providedMethod.CanWrite,
        myProvidedTypesManager.Register(providedMethod.PropertyType),
        providedMethod.Name,
        myProvidedTypesManager.Register(providedMethod.DeclaringType));

      return ppModel;
    }
  }
}
