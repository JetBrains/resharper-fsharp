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
      constructorInfo == null
        ? null
        : new ProxyProvidedConstructorInfoWithCache(constructorInfo, processModel, context, cache);

    public override string Name => myConstructorInfo.Name;
    public override bool IsAbstract => HasFlag(RdProvidedMethodFlags.IsAbstract);
    public override bool IsConstructor => HasFlag(RdProvidedMethodFlags.IsConstructor);
    public override bool IsFamily => HasFlag(RdProvidedMethodFlags.IsFamily);
    public override bool IsFinal => HasFlag(RdProvidedMethodFlags.IsFinal);
    public override bool IsPublic => HasFlag(RdProvidedMethodFlags.IsPublic);
    public override bool IsStatic => HasFlag(RdProvidedMethodFlags.IsStatic);
    public override bool IsVirtual => HasFlag(RdProvidedMethodFlags.IsVirtual);
    public override bool IsGenericMethod => HasFlag(RdProvidedMethodFlags.IsGenericMethod);
    public override bool IsFamilyAndAssembly => HasFlag(RdProvidedMethodFlags.IsFamilyAndAssembly);
    public override bool IsFamilyOrAssembly => HasFlag(RdProvidedMethodFlags.IsFamilyOrAssembly);
    public override bool IsHideBySig => HasFlag(RdProvidedMethodFlags.IsHideBySig);

    public override ProvidedParameterInfo[] GetStaticParametersForMethod(ITypeProvider provider) =>
      myStaticParameters.Value;

    public override ProvidedMethodBase ApplyStaticArgumentsForMethod(ITypeProvider provider,
      string fullNameAfterArguments,
      object[] staticArgs) =>
      Create(
        RdProvidedConstructorInfoProcessModel.ApplyStaticArgumentsForMethod.Sync(
          new ApplyStaticArgumentsForMethodArgs(EntityId, fullNameAfterArguments,
            staticArgs.Select(t => t.BoxToServerStaticArg()).ToArray())), myProcessModel, myContext, myCache);

    public override ProvidedType DeclaringType =>
      myCache.GetOrCreateWithContext(
        myDeclaringTypeId ??= RdProvidedConstructorInfoProcessModel.DeclaringType.Sync(EntityId), myContext);

    public override ProvidedParameterInfo[] GetParameters() => myParameters.Value;
    public override ProvidedType[] GetGenericArguments() => myGenericArguments.Value;

    private bool HasFlag(RdProvidedMethodFlags flag) => (myConstructorInfo.Flags & flag) == flag;

    private int? myDeclaringTypeId;
    private readonly Lazy<ProvidedParameterInfo[]> myParameters;
    private readonly Lazy<ProvidedType[]> myGenericArguments;
    private readonly Lazy<ProvidedParameterInfo[]> myStaticParameters;
  }
}
