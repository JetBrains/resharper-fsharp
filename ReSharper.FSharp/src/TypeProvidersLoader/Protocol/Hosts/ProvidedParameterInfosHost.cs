using System;
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

    private int GetParameterType(int entityId)
    {
      var (providedParameter, typeProviderId) = myProvidedParameterInfosCache.Get(entityId);
      return myProvidedTypesHost.CreateRdModel(providedParameter.ParameterType, typeProviderId).EntityId;
    }
  }
}
