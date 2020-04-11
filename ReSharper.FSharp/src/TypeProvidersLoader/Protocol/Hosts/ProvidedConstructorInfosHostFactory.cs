using System;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedConstructorInfosHostFactory : IOutOfProcessHostFactory<RdProvidedConstructorInfoProcessModel>
  {
    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedTypeRdModelsCreator;

    private readonly IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo>
      myProvidedParameterInfoRdModelsCreator;

    private readonly IReadProvidedCache<Tuple<ProvidedConstructorInfo, int>> myProvidedConstructorInfosCache;
    private readonly IReadProvidedCache<ITypeProvider> myTypeProvidersCache;

    public ProvidedConstructorInfosHostFactory(
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypeRdModelsCreator,
      IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo> providedParameterInfoRdModelsCreator,
      IReadProvidedCache<Tuple<ProvidedConstructorInfo, int>> providedConstructorInfosCache,
      IReadProvidedCache<ITypeProvider> typeProvidersCache)
    {
      myProvidedTypeRdModelsCreator = providedTypeRdModelsCreator;
      myProvidedParameterInfoRdModelsCreator = providedParameterInfoRdModelsCreator;
      myProvidedConstructorInfosCache = providedConstructorInfosCache;
      myTypeProvidersCache = typeProvidersCache;
    }

    public void Initialize(RdProvidedConstructorInfoProcessModel processModel)
    {
      processModel.DeclaringType.Set(GetDeclaringType);
      processModel.GetParameters.Set(GetParameters);
      processModel.GetGenericArguments.Set(GetGenericArguments);
      processModel.GetStaticParametersForMethod.Set(GetStaticParametersForMethod);
    }

    private RdTask<RdProvidedParameterInfo[]> GetStaticParametersForMethod(Lifetime lifetime, int entityId)
    {
      var (providedConstructor, typeProviderId) = myProvidedConstructorInfosCache.Get(entityId);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);
      var parameters = providedConstructor
        .GetStaticParametersForMethod(typeProvider)
        .Select(t => myProvidedParameterInfoRdModelsCreator.CreateRdModel(t, typeProviderId))
        .ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(parameters);
    }

    private RdTask<int?> GetDeclaringType(Lifetime lifetime, int entityId)
    {
      var (providedConstructor, typeProviderId) = myProvidedConstructorInfosCache.Get(entityId);
      var declaringType = myProvidedTypeRdModelsCreator.CreateRdModel(providedConstructor.DeclaringType, typeProviderId)
        ?.EntityId;
      return RdTask<int?>.Successful(declaringType);
    }

    private RdTask<int[]> GetGenericArguments(Lifetime lifetime, int entityId)
    {
      var (providedConstructor, typeProviderId) = myProvidedConstructorInfosCache.Get(entityId);
      var genericArgs = providedConstructor
        .GetGenericArguments()
        .Select(t => myProvidedTypeRdModelsCreator.CreateRdModel(t, typeProviderId).EntityId)
        .ToArray();
      return RdTask<int[]>.Successful(genericArgs);
    }

    private RdTask<RdProvidedParameterInfo[]> GetParameters(Lifetime lifetime, int entityId)
    {
      var (providedConstructor, typeProviderId) = myProvidedConstructorInfosCache.Get(entityId);
      var parameters = providedConstructor
        .GetParameters()
        .Select(t => myProvidedParameterInfoRdModelsCreator.CreateRdModel(t, typeProviderId))
        .ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(parameters);
    }
  }
}
