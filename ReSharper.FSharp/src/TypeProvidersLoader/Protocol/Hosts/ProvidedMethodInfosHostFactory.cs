using System;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedMethodInfosHostFactory : IOutOfProcessHostFactory<RdProvidedMethodInfoProcessModel>
  {
    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedTypeRdModelsCreator;

    private readonly IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo>
      myProvidedParameterInfoRdModelsCreator;

    private readonly IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo>
      myProvidedMethodInfoRdModelsCreator;

    private readonly IReadProvidedCache<ITypeProvider> myTypeProvidersCache;

    private readonly IReadProvidedCache<Tuple<ProvidedMethodInfo, int>> myProvidedMethodInfosCache;

    public ProvidedMethodInfosHostFactory(
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypeRdModelsCreator,
      IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo> providedParameterInfoRdModelsCreator,
      IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo> providedMethodInfoRdModelsCreator,
      IReadProvidedCache<ITypeProvider> typeProvidersCache,
      IReadProvidedCache<Tuple<ProvidedMethodInfo, int>> providedMethodInfosCache)
    {
      myProvidedTypeRdModelsCreator = providedTypeRdModelsCreator;
      myProvidedParameterInfoRdModelsCreator = providedParameterInfoRdModelsCreator;
      myProvidedMethodInfoRdModelsCreator = providedMethodInfoRdModelsCreator;
      myTypeProvidersCache = typeProvidersCache;
      myProvidedMethodInfosCache = providedMethodInfosCache;
    }

    public void Initialize(RdProvidedMethodInfoProcessModel processModel)
    {
      processModel.ReturnType.Set(GetReturnType);
      processModel.DeclaringType.Set(GetDeclaringType);
      processModel.GetParameters.Set(GetParameters);
      processModel.GetGenericArguments.Set(GetGenericArguments);
      processModel.GetStaticParametersForMethod.Set(GetStaticParametersForMethod);
      processModel.ApplyStaticArgumentsForMethod.Set(ApplyStaticArgumentsForMethod);
    }

    private RdTask<RdProvidedMethodInfo> ApplyStaticArgumentsForMethod(Lifetime lifetime,
      ApplyStaticArgumentsForMethodArgs args)
    {
      var (providedMethod, typeProviderId) = myProvidedMethodInfosCache.Get(args.EntityId);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);
      var methodInfo = myProvidedMethodInfoRdModelsCreator.CreateRdModel(
        providedMethod.ApplyStaticArgumentsForMethod(typeProvider, args.FullNameAfterArguments,
          args.StaticArgs.Select(t => t.Unbox()).ToArray()) as ProvidedMethodInfo, typeProviderId);
      return RdTask<RdProvidedMethodInfo>.Successful(methodInfo);
    }

    private RdTask<RdProvidedParameterInfo[]> GetStaticParametersForMethod(Lifetime lifetime, int entityId)
    {
      var (providedMethod, typeProviderId) = myProvidedMethodInfosCache.Get(entityId);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);
      var parameters = providedMethod.GetStaticParametersForMethod(typeProvider)
        .Select(t => myProvidedParameterInfoRdModelsCreator.CreateRdModel(t, typeProviderId))
        .ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(parameters);
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
      var returnType = myProvidedTypeRdModelsCreator.CreateRdModel(providedMethod.ReturnType, typeProviderId)
        .EntityId;
      return RdTask<int>.Successful(returnType);
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
