using System;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedEventInfoWithCache : ProvidedEventInfo
  {
    private readonly RdProvidedEventInfo myEventInfo;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    private readonly ITypeProviderCache myCache;
    private int EntityId => myEventInfo.EntityId;

    private RdProvidedEventInfoProcessModel RdProvidedEventInfoProcessModel =>
      myProcessModel.RdProvidedEventInfoProcessModel;

    public ProxyProvidedEventInfoWithCache(RdProvidedEventInfo eventInfo, RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context, ITypeProviderCache cache) : base(
      typeof(ProxyTypeProviderWithCache).GetEvents().First(), context)
    {
      myEventInfo = eventInfo;
      myProcessModel = processModel;
      myContext = context;
      myCache = cache;

      myAddMethod = new Lazy<ProvidedMethodInfo>(() =>
        ProxyProvidedMethodInfoWithCache.Create(RdProvidedEventInfoProcessModel.GetAddMethod.Sync(EntityId),
          myProcessModel, context, cache));

      myRemoveMethod = new Lazy<ProvidedMethodInfo>(() =>
        ProxyProvidedMethodInfoWithCache.Create(RdProvidedEventInfoProcessModel.GetRemoveMethod.Sync(EntityId),
          myProcessModel, context, cache));
    }

    [ContractAnnotation("eventInfo:null => null")]
    public static ProxyProvidedEventInfoWithCache Create(RdProvidedEventInfo eventInfo,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) =>
      eventInfo == null ? null : new ProxyProvidedEventInfoWithCache(eventInfo, processModel, context, cache);

    public override string Name => myEventInfo.Name;

    public override ProvidedType DeclaringType => myCache.GetOrCreateWithContext(
      myDeclaringTypeId ??= RdProvidedEventInfoProcessModel.DeclaringType.Sync(EntityId), myContext);

    public override ProvidedType EventHandlerType => myCache.GetOrCreateWithContext(
      myEventHandlerTypeId ??= RdProvidedEventInfoProcessModel.EventHandlerType.Sync(EntityId), myContext);

    public override ProvidedMethodInfo GetAddMethod() => myAddMethod.Value;

    public override ProvidedMethodInfo GetRemoveMethod() => myRemoveMethod.Value;

    private int? myDeclaringTypeId;
    private int? myEventHandlerTypeId;
    private readonly Lazy<ProvidedMethodInfo> myAddMethod;
    private readonly Lazy<ProvidedMethodInfo> myRemoveMethod;
  }
}
