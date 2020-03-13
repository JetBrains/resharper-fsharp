using System;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedMethodInfosHostFactory : IOutOfProcessHostFactory<RdProvidedMethodInfoProcessModel>
  {
    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedTypeRdModelsCreator;

    private readonly IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo>
      myProvidedParameterInfoRdModelsCreator;

    private readonly IReadProvidedCache<Tuple<ProvidedMethodInfo, int>> myProvidedMethodInfosCache;

    public ProvidedMethodInfosHostFactory(
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypeRdModelsCreator,
      IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo> providedParameterInfoRdModelsCreator,
      IReadProvidedCache<Tuple<ProvidedMethodInfo, int>> providedMethodInfosCache)
    {
      myProvidedTypeRdModelsCreator = providedTypeRdModelsCreator;
      myProvidedParameterInfoRdModelsCreator = providedParameterInfoRdModelsCreator;
      myProvidedMethodInfosCache = providedMethodInfosCache;
    }

    public void Initialize(RdProvidedMethodInfoProcessModel processModel)
    {
      processModel.ReturnType.Set(GetReturnType);
      processModel.DeclaringType.Set(GetDeclaringType);
      processModel.GetParameters.Set(GetParameters);
      processModel.GetGenericArguments.Set(GetGenericArguments);
    }

    private RdTask<int?> GetDeclaringType(Lifetime lifetime, int entityId)
    {
      var (providedMethod, typeProviderId) = myProvidedMethodInfosCache.Get(entityId);
      var declaringType = myProvidedTypeRdModelsCreator.CreateRdModel(providedMethod.DeclaringType, typeProviderId)
        ?.EntityId;
      return RdTask<int?>.Successful(declaringType);
    }

    private RdTask<int> GetReturnType(Lifetime lifetime, int entityId)
    {
      var (providedMethod, typeProviderId) = myProvidedMethodInfosCache.Get(entityId);
      var declaringType = myProvidedTypeRdModelsCreator.CreateRdModel(providedMethod.DeclaringType, typeProviderId)
        .EntityId;
      return RdTask<int>.Successful(declaringType);
    }

    private RdTask<int[]> GetGenericArguments(Lifetime lifetime, int entityId)
    {
      var (providedMethod, typeProviderId) = myProvidedMethodInfosCache.Get(entityId);
      var genericArgs = providedMethod
        .GetGenericArguments()
        .Select(t => myProvidedTypeRdModelsCreator.CreateRdModel(t, typeProviderId).EntityId)
        .ToArray();
      return RdTask<int[]>.Successful(genericArgs);
    }

    private RdTask<RdProvidedParameterInfo[]> GetParameters(Lifetime lifetime, int entityId)
    {
      var (providedMethod, typeProviderId) = myProvidedMethodInfosCache.Get(entityId);
      var parameters = providedMethod
        .GetParameters()
        .Select(t => myProvidedParameterInfoRdModelsCreator.CreateRdModel(t, typeProviderId))
        .ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(parameters);
    }
  }
}
