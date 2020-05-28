using System;
using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedMethodInfosHostFactory : IOutOfProcessHostFactory<RdProvidedMethodInfoProcessModel>
  {
    private readonly UnitOfWork myUnitOfWork;

    public ProvidedMethodInfosHostFactory(UnitOfWork unitOfWork)
    {
      myUnitOfWork = unitOfWork;
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
      var (providedMethod, typeProviderId) = myUnitOfWork.ProvidedMethodInfosCache.Get(args.EntityId);
      var typeProvider = myUnitOfWork.TypeProvidersCache.Get(typeProviderId);
      return myUnitOfWork.ProvidedMethodInfoRdModelsCreator.CreateRdModel(
        providedMethod.ApplyStaticArgumentsForMethod(typeProvider, args.FullNameAfterArguments,
          args.StaticArgs.Select(t => t.Unbox()).ToArray()) as ProvidedMethodInfo, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetStaticParametersForMethod(int entityId)
    {
      var (providedMethod, typeProviderId) = myUnitOfWork.ProvidedMethodInfosCache.Get(entityId);
      var typeProvider = myUnitOfWork.TypeProvidersCache.Get(typeProviderId);
      return providedMethod
        .GetStaticParametersForMethod(typeProvider)
        .CreateRdModels(myUnitOfWork.ProvidedParameterInfoRdModelsCreator, typeProviderId);
    }

    private int? GetDeclaringType(int entityId)
    {
      var (providedMethod, typeProviderId) = myUnitOfWork.ProvidedMethodInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedMethod.DeclaringType, typeProviderId)
        ?.EntityId;
    }

    private int GetReturnType(int entityId)
    {
      var (providedMethod, typeProviderId) = myUnitOfWork.ProvidedMethodInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedMethod.ReturnType, typeProviderId).EntityId;
    }

    private int[] GetGenericArguments(int entityId)
    {
      var (providedMethod, typeProviderId) = myUnitOfWork.ProvidedMethodInfosCache.Get(entityId);
      return providedMethod
        .GetGenericArguments()
        .CreateRdModelsAndReturnIds(myUnitOfWork.ProvidedTypeRdModelsCreator, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetParameters(int entityId)
    {
      var (providedMethod, typeProviderId) = myUnitOfWork.ProvidedMethodInfosCache.Get(entityId);
      return providedMethod
        .GetParameters()
        .CreateRdModels(myUnitOfWork.ProvidedParameterInfoRdModelsCreator, typeProviderId);
    }
  }
}
