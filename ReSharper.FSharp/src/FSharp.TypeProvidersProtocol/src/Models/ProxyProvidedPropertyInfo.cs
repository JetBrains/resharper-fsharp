﻿using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedPropertyInfo : ProvidedPropertyInfo
  {
    private readonly RdProvidedPropertyInfo myPropertyInfo;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    private readonly ITypeProviderCache myCache;
    private int EntityId => myPropertyInfo.EntityId;

    private RdProvidedPropertyInfoProcessModel RdProvidedPropertyInfoProcessModel =>
      myProcessModel.RdProvidedPropertyInfoProcessModel;

    private ProxyProvidedPropertyInfo(RdProvidedPropertyInfo propertyInfo,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) : base(
      typeof(string).GetProperties().First(), context)
    {
      myPropertyInfo = propertyInfo;
      myProcessModel = processModel;
      myContext = context;
      myCache = cache;
    }

    [ContractAnnotation("propertyInfo:null => null")]
    public static ProxyProvidedPropertyInfo CreateNoContext(RdProvidedPropertyInfo propertyInfo,
      RdFSharpTypeProvidersLoaderModel processModel, ITypeProviderCache cache) =>
      propertyInfo == null
        ? null
        : new ProxyProvidedPropertyInfo(propertyInfo, processModel, ProvidedTypeContext.Empty, cache);

    [ContractAnnotation("propertyInfo:null => null")]
    public static ProxyProvidedPropertyInfo Create(RdProvidedPropertyInfo propertyInfo,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) =>
      propertyInfo == null ? null : new ProxyProvidedPropertyInfo(propertyInfo, processModel, context, cache);

    public override string Name => myPropertyInfo.Name;
    public override bool CanRead => myPropertyInfo.CanRead;
    public override bool CanWrite => myPropertyInfo.CanWrite;

    public override ProvidedType DeclaringType =>
      myCache.GetOrCreateWithContext(RdProvidedPropertyInfoProcessModel.DeclaringType.Sync(EntityId),
        myContext);

    public override ProvidedType PropertyType =>
      myCache.GetOrCreateWithContext(RdProvidedPropertyInfoProcessModel.PropertyType.Sync(EntityId),
        myContext);

    public override ProvidedMethodInfo GetGetMethod() =>
      ProxyProvidedMethodInfo.Create(RdProvidedPropertyInfoProcessModel.GetGetMethod.Sync(EntityId), myProcessModel,
        myContext, myCache);

    public override ProvidedMethodInfo GetSetMethod() =>
      ProxyProvidedMethodInfo.Create(RdProvidedPropertyInfoProcessModel.GetSetMethod.Sync(EntityId), myProcessModel,
        myContext, myCache);

    public override ProvidedParameterInfo[] GetIndexParameters() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedPropertyInfoProcessModel.GetIndexParameters
        .Sync(EntityId)
        .Select(t => ProxyProvidedParameterInfoWithCache.Create(t, myProcessModel, myContext, myCache))
        .ToArray();
  }
}
