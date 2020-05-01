using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedPropertyInfoWithCache : ProvidedPropertyInfo
  {
    private readonly RdProvidedPropertyInfo myPropertyInfo;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    private readonly ITypeProviderCache myCache;
    private int EntityId => myPropertyInfo.EntityId;

    private RdProvidedPropertyInfoProcessModel RdProvidedPropertyInfoProcessModel =>
      myProcessModel.RdProvidedPropertyInfoProcessModel;

    private ProxyProvidedPropertyInfoWithCache(RdProvidedPropertyInfo propertyInfo,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) : base(
      typeof(string).GetProperties().First(), context)
    {
      myPropertyInfo = propertyInfo;
      myProcessModel = processModel;
      myContext = context;
      myCache = cache;

      myGetMethod = new Lazy<ProvidedMethodInfo>(() =>
        ProxyProvidedMethodInfoWithCache.Create(RdProvidedPropertyInfoProcessModel.GetGetMethod.Sync(EntityId),
          myProcessModel, myContext, myCache));

      mySetMethod = new Lazy<ProvidedMethodInfo>(() =>
        ProxyProvidedMethodInfoWithCache.Create(RdProvidedPropertyInfoProcessModel.GetSetMethod.Sync(EntityId),
          myProcessModel, myContext, myCache));

      myIndexParameters = new Lazy<ProvidedParameterInfo[]>(() => // ReSharper disable once CoVariantArrayConversion
        RdProvidedPropertyInfoProcessModel.GetIndexParameters
          .Sync(EntityId)
          .Select(t => ProxyProvidedParameterInfoWithCache.Create(t, myProcessModel, myContext, myCache))
          .ToArray());
    }

    [ContractAnnotation("propertyInfo:null => null")]
    public static ProxyProvidedPropertyInfoWithCache Create(RdProvidedPropertyInfo propertyInfo,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) =>
      propertyInfo == null ? null : new ProxyProvidedPropertyInfoWithCache(propertyInfo, processModel, context, cache);

    public override string Name => myPropertyInfo.Name;
    public override bool CanRead => myPropertyInfo.CanRead;
    public override bool CanWrite => myPropertyInfo.CanWrite;

    public override ProvidedType DeclaringType =>
      myCache.GetOrCreateWithContext(
        myDeclaringTypeId ??= RdProvidedPropertyInfoProcessModel.DeclaringType.Sync(EntityId),
        myContext);

    public override ProvidedType PropertyType =>
      myCache.GetOrCreateWithContext(
        myPropertyTypeId ??= RdProvidedPropertyInfoProcessModel.PropertyType.Sync(EntityId),
        myContext);

    public override ProvidedMethodInfo GetGetMethod() => myGetMethod.Value;

    public override ProvidedMethodInfo GetSetMethod() => mySetMethod.Value;

    public override ProvidedParameterInfo[] GetIndexParameters() => myIndexParameters.Value;

    private int? myDeclaringTypeId;
    private int? myPropertyTypeId;
    private readonly Lazy<ProvidedMethodInfo> myGetMethod;
    private readonly Lazy<ProvidedMethodInfo> mySetMethod;
    private readonly Lazy<ProvidedParameterInfo[]> myIndexParameters;
  }
}
