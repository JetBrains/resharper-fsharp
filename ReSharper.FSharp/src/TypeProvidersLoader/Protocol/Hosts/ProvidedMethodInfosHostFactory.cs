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

    private RdProvidedMethodInfo ApplyStaticArgumentsForMethod(ApplyStaticArgumentsForMethodArgs args)
    {
      var (providedMethod, typeProviderId) = myProvidedMethodInfosCache.Get(args.EntityId);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);
      return myProvidedMethodInfoRdModelsCreator.CreateRdModel(
        providedMethod.ApplyStaticArgumentsForMethod(typeProvider, args.FullNameAfterArguments,
          args.StaticArgs.Select(t => t.Unbox()).ToArray()) as ProvidedMethodInfo, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetStaticParametersForMethod(int entityId)
    {
      var (providedMethod, typeProviderId) = myProvidedMethodInfosCache.Get(entityId);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);
      return providedMethod
        .GetStaticParametersForMethod(typeProvider)
        .CreateRdModels(myProvidedParameterInfoRdModelsCreator, typeProviderId);
    }

    private int? GetDeclaringType(int entityId)
    {
      var (providedMethod, typeProviderId) = myProvidedMethodInfosCache.Get(entityId);
      return myProvidedTypeRdModelsCreator.CreateRdModel(providedMethod.DeclaringType, typeProviderId)?.EntityId;
    }

    private int GetReturnType(int entityId)
    {
      var (providedMethod, typeProviderId) = myProvidedMethodInfosCache.Get(entityId);
      return myProvidedTypeRdModelsCreator.CreateRdModel(providedMethod.ReturnType, typeProviderId).EntityId;
    }

    private int[] GetGenericArguments(int entityId)
    {
      var (providedMethod, typeProviderId) = myProvidedMethodInfosCache.Get(entityId);
      return providedMethod
        .GetGenericArguments()
        .CreateRdModelsAndReturnIds(myProvidedTypeRdModelsCreator, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetParameters(int entityId)
    {
      var (providedMethod, typeProviderId) = myProvidedMethodInfosCache.Get(entityId);
      return providedMethod
        .GetParameters()
        .CreateRdModels(myProvidedParameterInfoRdModelsCreator, typeProviderId);
    }
  }
}
