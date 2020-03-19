using System;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedEventInfoHostFactory : IOutOfProcessHostFactory<RdProvidedEventInfoProcessModel>
  {
    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedTypeRdModelsCreator;

    private readonly IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo>
      myProvidedMethodInfoRdModelsCreator;

    private readonly IReadProvidedCache<Tuple<ProvidedEventInfo, int>> myProvidedEventInfosCache;

    public ProvidedEventInfoHostFactory(
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypeRdModelsCreator,
      IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo> providedMethodInfoRdModelsCreator,
      IReadProvidedCache<Tuple<ProvidedEventInfo, int>> providedEventInfosCache)
    {
      myProvidedTypeRdModelsCreator = providedTypeRdModelsCreator;
      myProvidedMethodInfoRdModelsCreator = providedMethodInfoRdModelsCreator;
      myProvidedEventInfosCache = providedEventInfosCache;
    }

    public void Initialize(RdProvidedEventInfoProcessModel model)
    {
      model.DeclaringType.Set(GetDeclaringType);
      model.EventHandlerType.Set(GetEventHandlerType);
      model.GetAddMethod.Set(GetAddMethod);
      model.GetRemoveMethod.Set(GetRemoveMethod);
    }

    private RdTask<RdProvidedMethodInfo> GetRemoveMethod(Lifetime lifetime, int entityId)
    {
      var (providedEvent, typeProviderId) = myProvidedEventInfosCache.Get(entityId);
      var removeMethod =
        myProvidedMethodInfoRdModelsCreator.CreateRdModel(providedEvent.GetRemoveMethod(), typeProviderId);
      return RdTask<RdProvidedMethodInfo>.Successful(removeMethod);
    }

    private RdTask<RdProvidedMethodInfo> GetAddMethod(Lifetime lifetime, int entityId)
    {
      var (providedEvent, typeProviderId) = myProvidedEventInfosCache.Get(entityId);
      var addMethod =
        myProvidedMethodInfoRdModelsCreator.CreateRdModel(providedEvent.GetAddMethod(), typeProviderId);
      return RdTask<RdProvidedMethodInfo>.Successful(addMethod);
    }

    private RdTask<int> GetEventHandlerType(Lifetime lifetime, int entityId)
    {
      var (providedEvent, typeProviderId) = myProvidedEventInfosCache.Get(entityId);
      var eventHandlerTypeId = myProvidedTypeRdModelsCreator
        .CreateRdModel(providedEvent.EventHandlerType, typeProviderId).EntityId;
      return RdTask<int>.Successful(eventHandlerTypeId);
    }

    private RdTask<int?> GetDeclaringType(Lifetime lifetime, int entityId)
    {
      var (providedEvent, typeProviderId) = myProvidedEventInfosCache.Get(entityId);
      var declaringTypeId = myProvidedTypeRdModelsCreator.CreateRdModel(providedEvent.DeclaringType, typeProviderId)
        ?.EntityId;
      return RdTask<int?>.Successful(declaringTypeId);
    }
  }
}
