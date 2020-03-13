using System;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedParameterInfosHostFactory : IOutOfProcessHostFactory<RdProvidedParameterInfoProcessModel>
  {
    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedTypesHost;
    private readonly IReadProvidedCache<Tuple<ProvidedParameterInfo, int>> myProvidedParameterInfosCache;

    public ProvidedParameterInfosHostFactory(IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypesHost,
      IReadProvidedCache<Tuple<ProvidedParameterInfo, int>> providedParameterInfosCache)
    {
      myProvidedTypesHost = providedTypesHost;
      myProvidedParameterInfosCache = providedParameterInfosCache;
    }

    public void Initialize(RdProvidedParameterInfoProcessModel processModel)
    {
      processModel.ParameterType.Set(GetParameterType);
    }

    private RdTask<int> GetParameterType(Lifetime lifetime, int entityId)
    {
      var (providedParameter, typeProviderId) = myProvidedParameterInfosCache.Get(entityId);
      var parameterType = myProvidedTypesHost.CreateRdModel(providedParameter.ParameterType, typeProviderId).EntityId;
      return RdTask<int>.Successful(parameterType);
    }
  }
}
