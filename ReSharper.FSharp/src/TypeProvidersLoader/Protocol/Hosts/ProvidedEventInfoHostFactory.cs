using System;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedEventInfoHostFactory : IOutOfProcessHostFactory<RdProvidedEventInfoProcessModel>
  {
    private readonly UnitOfWork myUnitOfWork;

    public ProvidedEventInfoHostFactory(UnitOfWork unitOfWork)
    {
      myUnitOfWork = unitOfWork;
    }

    public void Initialize(RdProvidedEventInfoProcessModel model)
    {
      model.DeclaringType.Set(GetDeclaringType);
      model.EventHandlerType.Set(GetEventHandlerType);
      model.GetAddMethod.Set(GetAddMethod);
      model.GetRemoveMethod.Set(GetRemoveMethod);
    }

    private RdProvidedMethodInfo GetRemoveMethod(int entityId)
    {
      var (providedEvent, typeProviderId) = myUnitOfWork.ProvidedEventInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedMethodInfoRdModelsCreator.CreateRdModel(providedEvent.GetRemoveMethod(),
        typeProviderId);
    }

    private RdProvidedMethodInfo GetAddMethod(int entityId)
    {
      var (providedEvent, typeProviderId) = myUnitOfWork.ProvidedEventInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedMethodInfoRdModelsCreator.CreateRdModel(providedEvent.GetAddMethod(), typeProviderId);
    }

    private int GetEventHandlerType(int entityId)
    {
      var (providedEvent, typeProviderId) = myUnitOfWork.ProvidedEventInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedEvent.EventHandlerType, typeProviderId)
        .EntityId;
    }

    private int? GetDeclaringType(int entityId)
    {
      var (providedEvent, typeProviderId) = myUnitOfWork.ProvidedEventInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedEvent.DeclaringType, typeProviderId)
        ?.EntityId;
    }
  }
}
