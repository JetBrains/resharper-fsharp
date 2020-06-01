using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using JetBrains.Util.Concurrency;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedPropertyInfoWithCache : ProvidedPropertyInfo
  {
    private readonly RdProvidedPropertyInfo myPropertyInfo;
    private readonly int myTypeProviderId;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    private readonly IProvidedTypesCache myCache;
    private int EntityId => myPropertyInfo.EntityId;

    private RdProvidedPropertyInfoProcessModel RdProvidedPropertyInfoProcessModel =>
      myProcessModel.RdProvidedPropertyInfoProcessModel;

    private ProxyProvidedPropertyInfoWithCache(RdProvidedPropertyInfo propertyInfo, int typeProviderId,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, IProvidedTypesCache cache) : base(
      typeof(string).GetProperties().First(), context)
    {
      myPropertyInfo = propertyInfo;
      myTypeProviderId = typeProviderId;
      myProcessModel = processModel;
      myContext = context;
      myCache = cache;

      myGetMethod = new InterruptibleLazy<ProvidedMethodInfo>(() =>
        ProxyProvidedMethodInfoWithCache.Create(RdProvidedPropertyInfoProcessModel.GetGetMethod.Sync(EntityId),
          myTypeProviderId,
          myProcessModel, myContext, myCache));

      mySetMethod = new InterruptibleLazy<ProvidedMethodInfo>(() =>
        ProxyProvidedMethodInfoWithCache.Create(RdProvidedPropertyInfoProcessModel.GetSetMethod.Sync(EntityId),
          myTypeProviderId,
          myProcessModel, myContext, myCache));

      myIndexParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(
        () => // ReSharper disable once CoVariantArrayConversion
          RdProvidedPropertyInfoProcessModel.GetIndexParameters
            .Sync(EntityId)
            .Select(t =>
              ProxyProvidedParameterInfoWithCache.Create(t, myTypeProviderId, myProcessModel, myContext, myCache))
            .ToArray());
    }

    [ContractAnnotation("propertyInfo:null => null")]
    public static ProxyProvidedPropertyInfoWithCache Create(RdProvidedPropertyInfo propertyInfo, int typeProviderId,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, IProvidedTypesCache cache) =>
      propertyInfo == null
        ? null
        : new ProxyProvidedPropertyInfoWithCache(propertyInfo, typeProviderId, processModel, context, cache);

    public override string Name => myPropertyInfo.Name;
    public override bool CanRead => myPropertyInfo.CanRead;
    public override bool CanWrite => myPropertyInfo.CanWrite;

    public override ProvidedType DeclaringType =>
      myCache.GetOrCreateWithContext(
        myDeclaringTypeId ??= RdProvidedPropertyInfoProcessModel.DeclaringType.Sync(EntityId), myTypeProviderId,
        myContext);

    public override ProvidedType PropertyType =>
      myCache.GetOrCreateWithContext(
        myPropertyTypeId ??= RdProvidedPropertyInfoProcessModel.PropertyType.Sync(EntityId), myTypeProviderId,
        myContext);

    public override ProvidedMethodInfo GetGetMethod() => myGetMethod.Value;

    public override ProvidedMethodInfo GetSetMethod() => mySetMethod.Value;

    public override ProvidedParameterInfo[] GetIndexParameters() => myIndexParameters.Value;

    private int? myDeclaringTypeId;
    private int? myPropertyTypeId;
    private readonly InterruptibleLazy<ProvidedMethodInfo> myGetMethod;
    private readonly InterruptibleLazy<ProvidedMethodInfo> mySetMethod;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myIndexParameters;
  }
}
