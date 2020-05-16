using System;
using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
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

    private readonly IProvidedRdModelsCreator<ProvidedConstructorInfo, RdProvidedConstructorInfo>
      myProvidedConstructorInfoRdModelsCreator;

    private readonly IReadProvidedCache<Tuple<ProvidedConstructorInfo, int>> myProvidedConstructorInfosCache;
    private readonly IReadProvidedCache<ITypeProvider> myTypeProvidersCache;

    public ProvidedConstructorInfosHostFactory(
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypeRdModelsCreator,
      IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo> providedParameterInfoRdModelsCreator,
      IProvidedRdModelsCreator<ProvidedConstructorInfo, RdProvidedConstructorInfo>
        providedConstructorInfoRdModelsCreator,
      IReadProvidedCache<Tuple<ProvidedConstructorInfo, int>> providedConstructorInfosCache,
      IReadProvidedCache<ITypeProvider> typeProvidersCache)
    {
      myProvidedTypeRdModelsCreator = providedTypeRdModelsCreator;
      myProvidedParameterInfoRdModelsCreator = providedParameterInfoRdModelsCreator;
      myProvidedConstructorInfoRdModelsCreator = providedConstructorInfoRdModelsCreator;
      myProvidedConstructorInfosCache = providedConstructorInfosCache;
      myTypeProvidersCache = typeProvidersCache;
    }

    public void Initialize(RdProvidedConstructorInfoProcessModel processModel)
    {
      processModel.DeclaringType.Set(GetDeclaringType);
      processModel.GetParameters.Set(GetParameters);
      processModel.GetGenericArguments.Set(GetGenericArguments);
      processModel.GetStaticParametersForMethod.Set(GetStaticParametersForMethod);
      processModel.ApplyStaticArgumentsForMethod.Set(ApplyStaticArgumentsForMethod);
    }

    private RdProvidedConstructorInfo ApplyStaticArgumentsForMethod(ApplyStaticArgumentsForMethodArgs args)
    {
      var (providedConstructor, typeProviderId) = myProvidedConstructorInfosCache.Get(args.EntityId);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);
      return myProvidedConstructorInfoRdModelsCreator.CreateRdModel(
        providedConstructor.ApplyStaticArgumentsForMethod(typeProvider, args.FullNameAfterArguments,
          args.StaticArgs.Select(t => t.Unbox()).ToArray()) as ProvidedConstructorInfo, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetStaticParametersForMethod(int entityId)
    {
      var (providedConstructor, typeProviderId) = myProvidedConstructorInfosCache.Get(entityId);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);
      return providedConstructor
        .GetStaticParametersForMethod(typeProvider)
        .CreateRdModels(myProvidedParameterInfoRdModelsCreator, typeProviderId);
    }

    private int? GetDeclaringType(int entityId)
    {
      var (providedConstructor, typeProviderId) = myProvidedConstructorInfosCache.Get(entityId);
      return myProvidedTypeRdModelsCreator.CreateRdModel(providedConstructor.DeclaringType, typeProviderId)?.EntityId;
    }

    private int[] GetGenericArguments(int entityId)
    {
      var (providedConstructor, typeProviderId) = myProvidedConstructorInfosCache.Get(entityId);
      return providedConstructor
        .GetGenericArguments()
        .CreateRdModelsAndReturnIds(myProvidedTypeRdModelsCreator, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetParameters(int entityId)
    {
      var (providedConstructor, typeProviderId) = myProvidedConstructorInfosCache.Get(entityId);
      return providedConstructor
        .GetParameters()
        .CreateRdModels(myProvidedParameterInfoRdModelsCreator, typeProviderId);
    }
  }
}
