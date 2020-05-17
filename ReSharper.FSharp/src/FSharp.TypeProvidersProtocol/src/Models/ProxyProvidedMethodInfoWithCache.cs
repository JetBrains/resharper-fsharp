using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using JetBrains.Util.Concurrency;
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

      myParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(() => // ReSharper disable once CoVariantArrayConversion
        RdProvidedMethodInfoProcessModel.GetParameters
          .Sync(EntityId)
          .Select(t => ProxyProvidedParameterInfoWithCache.Create(t, myProcessModel, myContext, myCache))
          .ToArray());

      myStaticParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(() => // ReSharper disable once CoVariantArrayConversion
        RdProvidedMethodInfoProcessModel.GetStaticParametersForMethod
          .Sync(EntityId)
          .Select(t => ProxyProvidedParameterInfoWithCache.Create(t, myProcessModel, myContext, myCache))
          .ToArray());

      myGenericArguments = new InterruptibleLazy<ProvidedType[]>(() =>
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
    public override int MetadataToken => myMethodInfo.MetadataToken;

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

    private bool HasFlag(RdProvidedMethodFlags flag) => (myMethodInfo.Flags & flag) == flag;

    private int? myDeclaringTypeId;
    private int? myReturnTypeId;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myParameters;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myStaticParameters;
    private readonly InterruptibleLazy<ProvidedType[]> myGenericArguments;
  }
}
