using System;
using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedConstructorInfosHostFactory : IOutOfProcessHostFactory<RdProvidedConstructorInfoProcessModel>
  {
    private readonly UnitOfWork myUnitOfWork;

    public ProvidedConstructorInfosHostFactory(UnitOfWork unitOfWork)
    {
      myUnitOfWork = unitOfWork;
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
      var (providedConstructor, typeProviderId) = myUnitOfWork.ProvidedConstructorInfosCache.Get(args.EntityId);
      var typeProvider = myUnitOfWork.TypeProvidersCache.Get(typeProviderId);
      return myUnitOfWork.ProvidedConstructorInfoRdModelsCreator.CreateRdModel(
        providedConstructor.ApplyStaticArgumentsForMethod(typeProvider, args.FullNameAfterArguments,
          args.StaticArgs.Select(t => t.Unbox()).ToArray()) as ProvidedConstructorInfo, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetStaticParametersForMethod(int entityId)
    {
      var (providedConstructor, typeProviderId) = myUnitOfWork.ProvidedConstructorInfosCache.Get(entityId);
      var typeProvider = myUnitOfWork.TypeProvidersCache.Get(typeProviderId);
      return providedConstructor
        .GetStaticParametersForMethod(typeProvider)
        .CreateRdModels(myUnitOfWork.ProvidedParameterInfoRdModelsCreator, typeProviderId);
    }

    private int? GetDeclaringType(int entityId)
    {
      var (providedConstructor, typeProviderId) = myUnitOfWork.ProvidedConstructorInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedConstructor.DeclaringType, typeProviderId)
        ?.EntityId;
    }

    private int[] GetGenericArguments(int entityId)
    {
      var (providedConstructor, typeProviderId) = myUnitOfWork.ProvidedConstructorInfosCache.Get(entityId);
      return providedConstructor
        .GetGenericArguments()
        .CreateRdModelsAndReturnIds(myUnitOfWork.ProvidedTypeRdModelsCreator, typeProviderId);
    }

    private RdProvidedParameterInfo[] GetParameters(int entityId)
    {
      var (providedConstructor, typeProviderId) = myUnitOfWork.ProvidedConstructorInfosCache.Get(entityId);
      return providedConstructor
        .GetParameters()
        .CreateRdModels(myUnitOfWork.ProvidedParameterInfoRdModelsCreator, typeProviderId);
    }
  }
}
