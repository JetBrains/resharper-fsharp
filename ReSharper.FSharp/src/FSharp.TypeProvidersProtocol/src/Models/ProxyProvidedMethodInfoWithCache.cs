using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedMethodInfoWithCache : ProvidedMethodInfo, IRdProvidedEntity
  {
    private readonly RdProvidedMethodInfo myMethodInfo;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    private readonly ITypeProviderCache myCache;
    public int EntityId => myMethodInfo.EntityId;

    private RdProvidedMethodInfoProcessModel RdProvidedMethodInfoProcessModel =>
      myProcessModel.RdProvidedMethodInfoProcessModel;

    private ProxyProvidedMethodInfoWithCache(RdProvidedMethodInfo methodInfo,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context, ITypeProviderCache cache) : base(
      typeof(ProxyProvidedMethodInfoWithCache).GetMethods().First(),
      context)
    {
      myMethodInfo = methodInfo;
      myProcessModel = processModel;
      myContext = context;
      myCache = cache;

      myParameters = new Lazy<ProvidedParameterInfo[]>(() => // ReSharper disable once CoVariantArrayConversion
        RdProvidedMethodInfoProcessModel.GetParameters
          .Sync(EntityId)
          .Select(t => ProxyProvidedParameterInfoWithCache.Create(t, myProcessModel, myContext, myCache))
          .ToArray());

      myStaticParameters = new Lazy<ProvidedParameterInfo[]>(() => // ReSharper disable once CoVariantArrayConversion
        RdProvidedMethodInfoProcessModel.GetStaticParametersForMethod
          .Sync(EntityId)
          .Select(t => ProxyProvidedParameterInfoWithCache.Create(t, myProcessModel, myContext, myCache))
          .ToArray());

      myGenericArguments = new Lazy<ProvidedType[]>(() =>
        RdProvidedMethodInfoProcessModel.GetGenericArguments
          .Sync(EntityId)
          .Select(t => myCache.GetOrCreateWithContext(t, myContext))
          .ToArray());
    }

    [ContractAnnotation("methodInfo:null => null")]
    public static ProxyProvidedMethodInfoWithCache Create(
      RdProvidedMethodInfo methodInfo,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) =>
      methodInfo == null ? null : new ProxyProvidedMethodInfoWithCache(methodInfo, processModel, context, cache);

    public override string Name => myMethodInfo.Name;
    public override bool IsAbstract => myMethodInfo.IsAbstract;
    public override bool IsConstructor => myMethodInfo.IsConstructor;
    public override bool IsFamily => myMethodInfo.IsFamily;
    public override bool IsFinal => myMethodInfo.IsFinal;
    public override bool IsPublic => myMethodInfo.IsPublic;
    public override bool IsStatic => myMethodInfo.IsStatic;
    public override bool IsVirtual => myMethodInfo.IsVirtual;
    public override int MetadataToken => myMethodInfo.MetadataToken;
    public override bool IsGenericMethod => myMethodInfo.IsGenericMethod;
    public override bool IsFamilyAndAssembly => myMethodInfo.IsFamilyAndAssembly;
    public override bool IsFamilyOrAssembly => myMethodInfo.IsFamilyOrAssembly;
    public override bool IsHideBySig => myMethodInfo.IsHideBySig;

    public override ProvidedType DeclaringType =>
      myCache.GetOrCreateWithContext(
        myDeclaringTypeId ??= RdProvidedMethodInfoProcessModel.DeclaringType.Sync(EntityId), myContext);

    public override ProvidedType ReturnType =>
      myCache.GetOrCreateWithContext(
        myReturnTypeId ??= RdProvidedMethodInfoProcessModel.ReturnType.Sync(EntityId), myContext);

    public override ProvidedParameterInfo[] GetStaticParametersForMethod(ITypeProvider provider) =>
      myStaticParameters.Value;

    public override ProvidedMethodBase ApplyStaticArgumentsForMethod(ITypeProvider provider,
      string fullNameAfterArguments,
      object[] staticArgs) =>
      Create(
        RdProvidedMethodInfoProcessModel.ApplyStaticArgumentsForMethod.Sync(
          new ApplyStaticArgumentsForMethodArgs(EntityId, fullNameAfterArguments,
            staticArgs.Select(t => t.BoxToServerStaticArg()).ToArray())), myProcessModel, myContext, myCache);

    public override ProvidedParameterInfo[] GetParameters() => myParameters.Value;
    public override ProvidedType[] GetGenericArguments() => myGenericArguments.Value;

    private int? myDeclaringTypeId;
    private int? myReturnTypeId;
    private readonly Lazy<ProvidedParameterInfo[]> myParameters;
    private readonly Lazy<ProvidedParameterInfo[]> myStaticParameters;
    private readonly Lazy<ProvidedType[]> myGenericArguments;
  }
}
