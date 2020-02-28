using FSharp.Compiler;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedParametersManager : IOutOfProcessProtocolManager<ProvidedParameterInfo,
    RdProvidedParameterInfo>
  {
    private readonly IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> myProvidedTypesManager;

    public ProvidedParametersManager(
      IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> providedTypesManager)
    {
      myProvidedTypesManager = providedTypesManager;
    }

    public RdProvidedParameterInfo Register(ProvidedParameterInfo providedMethod)
    {
      var parameterModel = new RdProvidedParameterInfo(providedMethod.Name,
        myProvidedTypesManager.Register(providedMethod.ParameterType),
        providedMethod.IsIn,
        providedMethod.IsOut,
        providedMethod.IsOptional,
        providedMethod.HasDefaultValue);

      return parameterModel;
    }
  }
}
