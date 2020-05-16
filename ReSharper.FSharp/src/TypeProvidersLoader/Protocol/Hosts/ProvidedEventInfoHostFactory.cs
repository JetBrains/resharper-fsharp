using System;
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

    private RdProvidedMethodInfo GetRemoveMethod(int entityId)
    {
      var (providedEvent, typeProviderId) = myProvidedEventInfosCache.Get(entityId);
      return myProvidedMethodInfoRdModelsCreator.CreateRdModel(providedEvent.GetRemoveMethod(), typeProviderId);
    }

    private RdProvidedMethodInfo GetAddMethod(int entityId)
    {
      var (providedEvent, typeProviderId) = myProvidedEventInfosCache.Get(entityId);
      return myProvidedMethodInfoRdModelsCreator.CreateRdModel(providedEvent.GetAddMethod(), typeProviderId);
    }

    private int GetEventHandlerType(int entityId)
    {
      var (providedEvent, typeProviderId) = myProvidedEventInfosCache.Get(entityId);
      return myProvidedTypeRdModelsCreator.CreateRdModel(providedEvent.EventHandlerType, typeProviderId).EntityId;
    }

    private int? GetDeclaringType(int entityId)
    {
      var (providedEvent, typeProviderId) = myProvidedEventInfosCache.Get(entityId);
      return myProvidedTypeRdModelsCreator.CreateRdModel(providedEvent.DeclaringType, typeProviderId)?.EntityId;
    }
  }
}
