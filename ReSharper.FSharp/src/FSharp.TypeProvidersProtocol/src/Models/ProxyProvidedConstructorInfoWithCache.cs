using System.Collections.Generic;
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
  public class ProxyProvidedConstructorInfoWithCache : ProvidedConstructorInfo, IProxyProvidedNamedEntity
  {
    private readonly RdProvidedConstructorInfo myConstructorInfo;
    private readonly int myTypeProviderId;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    private readonly IProvidedTypesCache myCache;
    public int EntityId => myConstructorInfo.EntityId;

    private RdProvidedConstructorInfoProcessModel RdProvidedConstructorInfoProcessModel =>
      myProcessModel.RdProvidedConstructorInfoProcessModel;

    private ProxyProvidedConstructorInfoWithCache(RdProvidedConstructorInfo constructorInfo, int typeProviderId,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context, IProvidedTypesCache cache) : base(
      typeof(string).GetConstructors().First(),
      ProvidedTypeContext.Empty)
    {
      myConstructorInfo = constructorInfo;
      myTypeProviderId = typeProviderId;
      myProcessModel = processModel;
      myContext = context;
      myCache = cache;

      myParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(
        () => // ReSharper disable once CoVariantArrayConversion
          RdProvidedConstructorInfoProcessModel.GetParameters
            .Sync(EntityId)
            .Select(t => ProxyProvidedParameterInfoWithCache.Create(t, myTypeProviderId, myProcessModel, myContext, myCache))
            .ToArray());

      myGenericArguments = new InterruptibleLazy<ProvidedType[]>(() =>
        RdProvidedConstructorInfoProcessModel.GetGenericArguments
          .Sync(EntityId)
          .Select(t => myCache.GetOrCreateWithContext(t, myTypeProviderId, myContext))
          .ToArray());

      myStaticParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(() =>
        // ReSharper disable once CoVariantArrayConversion
        RdProvidedConstructorInfoProcessModel.GetStaticParametersForMethod
          .Sync(EntityId)
          .Select(t => ProxyProvidedParameterInfoWithCache.Create(t, myTypeProviderId, myProcessModel, myContext, myCache))
          .ToArray());

      myGeneratedConstructorsCache = new Dictionary<string, ProxyProvidedConstructorInfoWithCache>();
    }

    [ContractAnnotation("constructorInfo:null => null")]
    public static ProxyProvidedConstructorInfoWithCache Create(
      int typeProviderId,
      RdProvidedConstructorInfo constructorInfo,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, IProvidedTypesCache cache) =>
      constructorInfo == null
        ? null
        : new ProxyProvidedConstructorInfoWithCache(constructorInfo, typeProviderId, processModel, context, cache);

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
      object[] staticArgs)
    {
      var key = string.Concat(staticArgs);
      if (!myGeneratedConstructorsCache.TryGetValue(key, out var method))
      {
        var staticArgDescriptions = staticArgs.Select(t => t.BoxToServerStaticArg()).ToArray();

        method = Create(myTypeProviderId, RdProvidedConstructorInfoProcessModel.ApplyStaticArgumentsForMethod.Sync(
            new ApplyStaticArgumentsForMethodArgs(EntityId, fullNameAfterArguments, staticArgDescriptions)),
          myProcessModel, myContext, myCache);

        myGeneratedConstructorsCache.Add(key, method);
      }

      return method;
    }

    public override ProvidedType DeclaringType =>
      myCache.GetOrCreateWithContext(
        myDeclaringTypeId ??= RdProvidedConstructorInfoProcessModel.DeclaringType.Sync(EntityId), myTypeProviderId,
        myContext);

    public override ProvidedParameterInfo[] GetParameters() => myParameters.Value;
    public override ProvidedType[] GetGenericArguments() => myGenericArguments.Value;
    public string FullName => DeclaringType.FullName + Name;

    private bool HasFlag(RdProvidedMethodFlags flag) => (myConstructorInfo.Flags & flag) == flag;

    private int? myDeclaringTypeId;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myParameters;
    private readonly InterruptibleLazy<ProvidedType[]> myGenericArguments;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myStaticParameters;
    private readonly Dictionary<string, ProxyProvidedConstructorInfoWithCache> myGeneratedConstructorsCache;
  }
}
