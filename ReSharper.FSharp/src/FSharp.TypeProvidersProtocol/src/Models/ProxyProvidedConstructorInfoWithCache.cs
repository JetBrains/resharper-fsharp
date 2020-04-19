using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedConstructorInfoWithCache : ProvidedConstructorInfo, IRdProvidedEntity
  {
    private readonly RdProvidedConstructorInfo myConstructorInfo;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    private readonly ITypeProviderCache myCache;
    public int EntityId => myConstructorInfo.EntityId;

    private RdProvidedConstructorInfoProcessModel RdProvidedConstructorInfoProcessModel =>
      myProcessModel.RdProvidedConstructorInfoProcessModel;

    private ProxyProvidedConstructorInfoWithCache(RdProvidedConstructorInfo constructorInfo,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context, ITypeProviderCache cache) : base(
      typeof(string).GetConstructors().First(),
      ProvidedTypeContext.Empty)
    {
      myConstructorInfo = constructorInfo;
      myProcessModel = processModel;
      myContext = context;
      myCache = cache;

      myParameters = new Lazy<ProvidedParameterInfo[]>(() => // ReSharper disable once CoVariantArrayConversion
        RdProvidedConstructorInfoProcessModel.GetParameters
          .Sync(EntityId)
          .Select(t => ProxyProvidedParameterInfoWithCache.Create(t, myProcessModel, myContext, myCache))
          .ToArray());

      myGenericArguments = new Lazy<ProvidedType[]>(() =>
        RdProvidedConstructorInfoProcessModel.GetGenericArguments
          .Sync(EntityId)
          .Select(t => myCache.GetOrCreateWithContext(t, myContext))
          .ToArray());

      myStaticParameters = new Lazy<ProvidedParameterInfo[]>(() =>
        // ReSharper disable once CoVariantArrayConversion
        RdProvidedConstructorInfoProcessModel.GetStaticParametersForMethod
          .Sync(EntityId)
          .Select(t => ProxyProvidedParameterInfoWithCache.Create(t, myProcessModel, myContext, myCache))
          .ToArray());
    }

    [ContractAnnotation("constructorInfo:null => null")]
    public static ProxyProvidedConstructorInfoWithCache Create(
      RdProvidedConstructorInfo constructorInfo,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) =>
      constructorInfo == null ? null : new ProxyProvidedConstructorInfoWithCache(constructorInfo, processModel, context, cache);

    public override string Name => myConstructorInfo.Name;
    public override bool IsAbstract => myConstructorInfo.IsAbstract;
    public override bool IsConstructor => myConstructorInfo.IsConstructor;
    public override bool IsFamily => myConstructorInfo.IsFamily;
    public override bool IsFinal => myConstructorInfo.IsFinal;
    public override bool IsPublic => myConstructorInfo.IsPublic;
    public override bool IsStatic => myConstructorInfo.IsStatic;
    public override bool IsVirtual => myConstructorInfo.IsVirtual;
    public override bool IsGenericMethod => myConstructorInfo.IsGenericMethod;
    public override bool IsFamilyAndAssembly => myConstructorInfo.IsFamilyAndAssembly;
    public override bool IsFamilyOrAssembly => myConstructorInfo.IsFamilyOrAssembly;
    public override bool IsHideBySig => myConstructorInfo.IsHideBySig;

    public override ProvidedParameterInfo[] GetStaticParametersForMethod(ITypeProvider provider) =>
      myStaticParameters.Value;

    public override ProvidedType DeclaringType =>
      myCache.GetOrCreateWithContext(
        myDeclaringTypeId ??= RdProvidedConstructorInfoProcessModel.DeclaringType.Sync(EntityId), myContext);

    public override ProvidedParameterInfo[] GetParameters() => myParameters.Value;
    public override ProvidedType[] GetGenericArguments() => myGenericArguments.Value;

    private int? myDeclaringTypeId;
    private readonly Lazy<ProvidedParameterInfo[]> myParameters;
    private readonly Lazy<ProvidedType[]> myGenericArguments;
    private readonly Lazy<ProvidedParameterInfo[]> myStaticParameters;
  }
}
