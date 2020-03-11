using System;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedType : ProvidedType
  {
    private readonly RdProvidedType myRdProvidedType;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ITypeProviderCache myCache;
    private int EntityId => myRdProvidedType.EntityId;
    private RdProvidedTypeProcessModel RdProvidedTypeProcessModel => myProcessModel.RdProvidedTypeProcessModel;

    internal ProxyProvidedType(RdProvidedType rdProvidedType, RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context, ITypeProviderCache cache) : base(
      typeof(string), context)
    {
      myRdProvidedType = rdProvidedType;
      myProcessModel = processModel;
      myCache = cache;
    }

    [ContractAnnotation("type:null => null")]
    public static ProxyProvidedType CreateNoContext(
      RdProvidedType type,
      RdFSharpTypeProvidersLoaderModel processModel,
      ITypeProviderCache cache) =>
      type == null ? null : new ProxyProvidedType(type, processModel, ProvidedTypeContext.Empty, cache);

    [ContractAnnotation("type:null => null")]
    public static ProxyProvidedType Create(
      RdProvidedType type,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context,
      ITypeProviderCache cache) =>
      type == null ? null : new ProxyProvidedType(type, processModel, context, cache);

    public override string Name => myRdProvidedType.Name;
    public override string FullName => myRdProvidedType.FullName;
    public override string Namespace => myRdProvidedType.Namespace;
    public override bool IsGenericParameter => myRdProvidedType.IsGenericParameter;
    public override bool IsValueType => myRdProvidedType.IsValueType;
    public override bool IsByRef => myRdProvidedType.IsByRef;
    public override bool IsPointer => myRdProvidedType.IsPointer;
    public override bool IsPublic => myRdProvidedType.IsPublic;
    public override bool IsNestedPublic => myRdProvidedType.IsNestedPublic;
    public override bool IsArray => myRdProvidedType.IsArray;
    public override bool IsEnum => myRdProvidedType.IsEnum;
    public override bool IsClass => myRdProvidedType.IsClass;
    public override bool IsSealed => myRdProvidedType.IsSealed;
    public override bool IsAbstract => myRdProvidedType.IsAbstract;
    public override bool IsInterface => myRdProvidedType.IsInterface;
    public override bool IsSuppressRelocate => myRdProvidedType.IsSuppressRelocate;
    public override bool IsErased => myRdProvidedType.IsErased;
    public override bool IsGenericType => myRdProvidedType.IsGenericType;

    public override int GenericParameterPosition =>
      RdProvidedTypeProcessModel.GenericParameterPosition.Sync(myRdProvidedType.EntityId);

    public override ProvidedType BaseType =>
      myCache.GetOrCreateWithContext(RdProvidedTypeProcessModel.BaseType.Sync(EntityId), Context);

    public override ProvidedType DeclaringType =>
      myCache.GetOrCreateWithContext(RdProvidedTypeProcessModel.DeclaringType.Sync(EntityId), Context);

    public override ProvidedType GetNestedType(string nm) =>
      myCache.GetOrCreateWithContext(
        RdProvidedTypeProcessModel.GetNestedType.Sync(new GetNestedTypeArgs(EntityId, nm)), Context);

    public override ProvidedType[] GetNestedTypes() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetNestedTypes
        .Sync(EntityId)
        .Select(t => myCache.GetOrCreateWithContext(t, Context))
        .ToArray();

    public override ProvidedType[] GetAllNestedTypes() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetAllNestedTypes
        .Sync(EntityId)
        .Select(t => myCache.GetOrCreateWithContext(t, Context))
        .ToArray();

    public override ProvidedType GetGenericTypeDefinition() =>
      myCache.GetOrCreateWithContext(RdProvidedTypeProcessModel.GetGenericTypeDefinition.Sync(EntityId),
        Context);

    public override ProvidedPropertyInfo[] GetProperties() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetProperties
        .Sync(EntityId)
        .Select(t => ProxyProvidedPropertyInfo.Create(t, myProcessModel, Context, myCache))
        .ToArray();

    public override ProvidedPropertyInfo GetProperty(string nm) =>
      ProxyProvidedPropertyInfo.Create(RdProvidedTypeProcessModel.GetProperty.Sync(new GetPropertyArgs(EntityId, nm)),
        myProcessModel, Context, myCache);

    public override int GetArrayRank() => RdProvidedTypeProcessModel.GetArrayRank.Sync(EntityId);

    public override ProvidedType GetElementType() =>
      myCache.GetOrCreateWithContext(RdProvidedTypeProcessModel.GetElementType.Sync(EntityId), Context);

    public override ProvidedType[] GetGenericArguments() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetGenericArguments
        .Sync(EntityId)
        .Select(t => myCache.GetOrCreateWithContext(t, Context))
        .ToArray();

    public override ProvidedType GetEnumUnderlyingType() =>
      myCache.GetOrCreateWithContext(RdProvidedTypeProcessModel.GetEnumUnderlyingType.Sync(EntityId),
        Context);

    public override ProvidedParameterInfo[] GetStaticParameters(ITypeProvider provider) =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetStaticParameters
        .Sync(EntityId)
        .Select(t => ProxyProvidedParameterInfoWithCache.Create(t, myProcessModel, Context, myCache))
        .ToArray();

    public override ProvidedType ApplyStaticArguments(ITypeProvider provider, string[] fullTypePathAfterArguments,
      object[] staticArgs)
    {
      var staticArgDescriptions = staticArgs.Select(t => t switch
      {
        sbyte x => new StaticArg("sbyte", x.ToString()),
        short x => new StaticArg("short", x.ToString()),
        int x => new StaticArg("int", x.ToString()),
        long x => new StaticArg("long", x.ToString()),
        byte x => new StaticArg("byte", x.ToString()),
        ushort x => new StaticArg("ushort", x.ToString()),
        uint x => new StaticArg("uint", x.ToString()),
        ulong x => new StaticArg("ulong", x.ToString()),
        decimal x => new StaticArg("decimal", x.ToString(CultureInfo.InvariantCulture)),
        float x => new StaticArg("float", x.ToString(CultureInfo.InvariantCulture)),
        double x => new StaticArg("double", x.ToString(CultureInfo.InvariantCulture)),
        char x => new StaticArg("char", x.ToString()),
        bool x => new StaticArg("bool", x.ToString()),
        string x => new StaticArg("string", x),
        _ => throw new ArgumentException($"Unexpected static arg with type {t.GetType().FullName}")
      }).ToArray();

      return myCache.GetOrCreateWithContext(
        RdProvidedTypeProcessModel.ApplyStaticArguments.Sync(
          new ApplyStaticArgumentsParameters(EntityId, fullTypePathAfterArguments, staticArgDescriptions)), Context);
    }

    public override ProvidedType[] GetInterfaces() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetInterfaces
        .Sync(EntityId)
        .Select(t => myCache.GetOrCreateWithContext(t, Context))
        .ToArray();

    public override ProvidedMethodInfo[] GetMethods() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetMethods
        .Sync(EntityId)
        .Select(t => ProxyProvidedMethodInfo.Create(t, myProcessModel, Context, myCache))
        .ToArray();

    public override ProvidedType ApplyContext(ProvidedTypeContext context) =>
      new ProxyProvidedType(myRdProvidedType, myProcessModel, context, myCache);
  }
}
