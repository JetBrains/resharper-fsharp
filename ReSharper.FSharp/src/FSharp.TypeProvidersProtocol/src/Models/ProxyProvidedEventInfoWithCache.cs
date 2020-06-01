using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using JetBrains.Util.Concurrency;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedEventInfoWithCache : ProvidedEventInfo
  {
    private readonly RdProvidedEventInfo myEventInfo;
    private readonly int myTypeProviderId;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    private readonly IProvidedTypesCache myCache;
    private int EntityId => myEventInfo.EntityId;

    private RdProvidedEventInfoProcessModel RdProvidedEventInfoProcessModel =>
      myProcessModel.RdProvidedEventInfoProcessModel;

    private ProxyProvidedEventInfoWithCache(RdProvidedEventInfo eventInfo, int typeProviderId,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context, IProvidedTypesCache cache) : base(
      typeof(ProxyTypeProviderWithCache).GetEvents().First(), context)
    {
      myEventInfo = eventInfo;
      myTypeProviderId = typeProviderId;
      myProcessModel = processModel;
      myContext = context;
      myCache = cache;

      myAddMethod = new InterruptibleLazy<ProvidedMethodInfo>(() =>
        ProxyProvidedMethodInfoWithCache.Create(RdProvidedEventInfoProcessModel.GetAddMethod.Sync(EntityId), myTypeProviderId,
          myProcessModel, context, cache));

      myRemoveMethod = new InterruptibleLazy<ProvidedMethodInfo>(() =>
        ProxyProvidedMethodInfoWithCache.Create(RdProvidedEventInfoProcessModel.GetRemoveMethod.Sync(EntityId), myTypeProviderId,
          myProcessModel, context, cache));
    }

    [ContractAnnotation("eventInfo:null => null")]
    public static ProxyProvidedEventInfoWithCache Create(RdProvidedEventInfo eventInfo, int typeProviderId,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, IProvidedTypesCache cache) =>
      eventInfo == null
        ? null
        : new ProxyProvidedEventInfoWithCache(eventInfo, typeProviderId, processModel, context, cache);

    public override string Name => myEventInfo.Name;

    public override ProvidedType DeclaringType => myCache.GetOrCreateWithContext(
      myDeclaringTypeId ??= RdProvidedEventInfoProcessModel.DeclaringType.Sync(EntityId), myTypeProviderId, myContext);

    public override ProvidedType EventHandlerType => myCache.GetOrCreateWithContext(
      myEventHandlerTypeId ??= RdProvidedEventInfoProcessModel.EventHandlerType.Sync(EntityId), myTypeProviderId,
      myContext);

    public override ProvidedMethodInfo GetAddMethod() => myAddMethod.Value;

    public override ProvidedMethodInfo GetRemoveMethod() => myRemoveMethod.Value;

    private int? myDeclaringTypeId;
    private int? myEventHandlerTypeId;
    private readonly InterruptibleLazy<ProvidedMethodInfo> myAddMethod;
    private readonly InterruptibleLazy<ProvidedMethodInfo> myRemoveMethod;
  }
}
