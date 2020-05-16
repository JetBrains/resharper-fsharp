using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedTypeWithCache : ProvidedType, IRdProvidedEntity
  {
    private readonly RdProvidedType myRdProvidedType;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ITypeProviderCache myCache;
    private ProvidedTypeContext myContext;
    public int EntityId => myRdProvidedType.EntityId;
    private RdProvidedTypeProcessModel RdProvidedTypeProcessModel => myProcessModel.RdProvidedTypeProcessModel;

    internal ProxyProvidedTypeWithCache(RdProvidedType rdProvidedType, RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context, ITypeProviderCache cache) : base(
      typeof(ProxyProvidedTypeWithCache), context)
    {
      myRdProvidedType = rdProvidedType;
      myProcessModel = processModel;
      myContext = context;
      myCache = cache;

      myInterfaces = new Lazy<ProvidedType[]>(() =>
        RdProvidedTypeProcessModel.GetInterfaces
          .Sync(EntityId)
          .Select(t => myCache.GetOrCreateWithContext(t, Context))
          .ToArray());

      myGenericArguments = new Lazy<ProvidedType[]>(() =>
        RdProvidedTypeProcessModel.GetGenericArguments
          .Sync(EntityId)
          .Select(t => myCache.GetOrCreateWithContext(t, Context))
          .ToArray());

      myMethods = new Lazy<ProvidedMethodInfo[]>(() =>
        // ReSharper disable once CoVariantArrayConversion
        RdProvidedTypeProcessModel.GetMethods
          .Sync(EntityId)
          .Select(t => ProxyProvidedMethodInfoWithCache.Create(t, myProcessModel, Context, myCache))
          .ToArray());

      myAllNestedTypes = new Lazy<ProvidedType[]>(() =>
        RdProvidedTypeProcessModel.GetNestedTypes
          .Sync(EntityId)
          .Select(t => myCache.GetOrCreateWithContext(t, Context))
          .ToArray());

      myProperties = new Lazy<ProvidedPropertyInfo[]>(() =>
        // ReSharper disable once CoVariantArrayConversion
        RdProvidedTypeProcessModel.GetProperties
          .Sync(EntityId)
          .Select(t => ProxyProvidedPropertyInfoWithCache.Create(t, myProcessModel, Context, myCache))
          .ToArray());

      myProvidedAssembly = new Lazy<ProvidedAssembly>(() => ProxyProvidedAssembly.CreateWithContext(
        RdProvidedTypeProcessModel.Assembly.Sync(EntityId), myProcessModel, Context));

      myStaticParameters = new Lazy<ProvidedParameterInfo[]>(() =>
        // ReSharper disable once CoVariantArrayConversion
        RdProvidedTypeProcessModel.GetStaticParameters
          .Sync(EntityId)
          .Select(t => ProxyProvidedParameterInfoWithCache.Create(t, myProcessModel, Context, myCache))
          .ToArray());

      myFields = new Lazy<ProvidedFieldInfo[]>(() =>
        // ReSharper disable once CoVariantArrayConversion
        RdProvidedTypeProcessModel.GetFields
          .Sync(EntityId)
          .Select(t => ProxyProvidedFieldInfoWithCache.Create(t, myProcessModel, Context, myCache))
          .ToArray());

      myEvents = new Lazy<ProvidedEventInfo[]>(() =>
        // ReSharper disable once CoVariantArrayConversion
        RdProvidedTypeProcessModel.GetEvents
          .Sync(EntityId)
          .Select(t => ProxyProvidedEventInfoWithCache.Create(t, myProcessModel, Context, myCache))
          .ToArray());

      myConstructors = new Lazy<ProvidedConstructorInfo[]>(() =>
        // ReSharper disable once CoVariantArrayConversion
        RdProvidedTypeProcessModel.GetConstructors
          .Sync(EntityId)
          .Select(t => ProxyProvidedConstructorInfoWithCache.Create(t, myProcessModel, Context, myCache))
          .ToArray());

      myDeclaringTypeId = new Lazy<int?>(() => RdProvidedTypeProcessModel.DeclaringType.Sync(EntityId));

      myTypeAsVarsCache = new Dictionary<string, ProvidedVar>();
      myGeneratedTypesCache = new Dictionary<string, ProvidedType>();
    }

    [ContractAnnotation("type:null => null")]
    public static ProxyProvidedTypeWithCache Create(
      RdProvidedType type,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context,
      ITypeProviderCache cache) =>
      type == null ? null : new ProxyProvidedTypeWithCache(type, processModel, context, cache);

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
    public override bool IsVoid => myRdProvidedType.IsVoid;
    public override bool IsMeasure => myRdProvidedType.IsMeasure;

    public override int GenericParameterPosition =>
      myGenericParameterPosition ??=
        RdProvidedTypeProcessModel.GenericParameterPosition.Sync(myRdProvidedType.EntityId);

    public override ProvidedType BaseType =>
      myCache.GetOrCreateWithContext(myBaseTypeId ??= RdProvidedTypeProcessModel.BaseType.Sync(EntityId), Context);

    public override ProvidedType DeclaringType =>
      myCache.GetOrCreateWithContext(myDeclaringTypeId.Value, Context);

    public override ProvidedType GetNestedType(string nm) =>
      myAllNestedTypes.Value.FirstOrDefault(t => t.Name == nm);

    //TODO: hide non public
    public override ProvidedType[] GetNestedTypes() => myAllNestedTypes.Value;

    public override ProvidedType[] GetAllNestedTypes() => myAllNestedTypes.Value;

    public override ProvidedType GetGenericTypeDefinition() =>
      myCache.GetOrCreateWithContext(
        myGenericTypeDefinitionId ??= RdProvidedTypeProcessModel.GetGenericTypeDefinition.Sync(EntityId), Context);

    public override ProvidedPropertyInfo[] GetProperties() => myProperties.Value;

    public override ProvidedPropertyInfo GetProperty(string nm) =>
      myProperties.Value.FirstOrDefault(t => t.Name == nm);

    public override int GetArrayRank() =>
      (myArrayRank ??= RdProvidedTypeProcessModel.GetArrayRank.Sync(EntityId));

    public override ProvidedType GetElementType() =>
      myCache.GetOrCreateWithContext(myElementTypeId ??= RdProvidedTypeProcessModel.GetElementType.Sync(EntityId),
        Context);

    public override ProvidedType[] GetGenericArguments() => myGenericArguments.Value;

    public override ProvidedType GetEnumUnderlyingType() =>
      myCache.GetOrCreateWithContext(
        myEnumUnderlyingTypeId ??= RdProvidedTypeProcessModel.GetEnumUnderlyingType.Sync(EntityId), Context);

    public override ProvidedParameterInfo[] GetStaticParameters(ITypeProvider provider) => myStaticParameters.Value;

    //TODO: use cache with lifetime invalidation
    public override ProvidedType ApplyStaticArguments(ITypeProvider provider, string[] fullTypePathAfterArguments,
      object[] staticArgs)
    {
      var key = string.Concat(fullTypePathAfterArguments);
      if (!myGeneratedTypesCache.TryGetValue(key, out var type))
      {
        var staticArgDescriptions = staticArgs.Select(t => t.BoxToServerStaticArg()).ToArray();

        type = myCache.GetOrCreateWithContext(
          RdProvidedTypeProcessModel.ApplyStaticArguments.Sync(
            new ApplyStaticArgumentsParameters(EntityId, fullTypePathAfterArguments, staticArgDescriptions)), Context);
        myGeneratedTypesCache.Add(key, type);
      }

      return type;
    }

    public override ProvidedType[] GetInterfaces() => myInterfaces.Value;

    public override ProvidedMethodInfo[] GetMethods() => myMethods.Value;

    public override ProvidedType MakeArrayType() =>
      myCache.GetOrCreateWithContext(
        myMakeArrayTypeId ??= RdProvidedTypeProcessModel.MakeArrayType.Sync(new MakeArrayTypeArgs(EntityId, 1)),
        Context);

    public override ProvidedType MakeArrayType(int rank) =>
      myCache.GetOrCreateWithContext(
        RdProvidedTypeProcessModel.MakeArrayType.Sync(new MakeArrayTypeArgs(EntityId, rank)),
        Context);

    public override ProvidedType MakeGenericType(ProvidedType[] args)
    {
      var proxyProvidedTypes = args.Select(t => t as IRdProvidedEntity);
      Assertion.Assert(args.All(t => t != null), "ProvidedType must be ProxyProvidedType");
      // ReSharper disable once PossibleNullReferenceException
      var argIds = proxyProvidedTypes.Select(t => t.EntityId).ToArray();
      return myCache.GetOrCreateWithContext(
        RdProvidedTypeProcessModel.MakeGenericType.Sync(new MakeGenericTypeArgs(EntityId, argIds)),
        Context);
    }

    public override ProvidedType MakePointerType() =>
      myCache.GetOrCreateWithContext(
        myMakePointerTypeId ??= RdProvidedTypeProcessModel.MakePointerType.Sync(EntityId),
        Context);

    public override ProvidedType MakeByRefType() =>
      myCache.GetOrCreateWithContext(
        myMakeByRefTypeId ??= RdProvidedTypeProcessModel.MakeByRefType.Sync(EntityId),
        Context);

    public override ProvidedEventInfo[] GetEvents() => myEvents.Value;

    public override ProvidedEventInfo GetEvent(string nm) => myEvents.Value.FirstOrDefault(t => t.Name == nm);

    public override ProvidedFieldInfo[] GetFields() => myFields.Value;

    public override ProvidedFieldInfo GetField(string nm) =>
      myFields.Value.FirstOrDefault(t => t.Name == nm);

    public override ProvidedConstructorInfo[] GetConstructors() => myConstructors.Value;

    public override ProvidedType ApplyContext(ProvidedTypeContext context)
    {
      var (lookupIlTypeRef, lookupTyconRef) = myContext.GetDictionaries();
      var (newLookupIlTypeRef, newLookupTyconRef) = context.GetDictionaries();

      foreach (var ilTypeRef in lookupIlTypeRef.Where(ilTypeRef => !newLookupIlTypeRef.ContainsKey(ilTypeRef.Key)))
      {
        newLookupIlTypeRef.Add(ilTypeRef.Key, ilTypeRef.Value);
      }

      foreach (var tyconRef in lookupTyconRef.Where(tyconRef => !newLookupTyconRef.ContainsKey(tyconRef.Key)))
      {
        newLookupTyconRef.Add(tyconRef.Key, tyconRef.Value);
      }

      myContext = context;
      return this;
    }

    public override ProvidedAssembly Assembly => myProvidedAssembly.Value;

    public override ProvidedVar AsProvidedVar(string nm)
    {
      if (!myTypeAsVarsCache.TryGetValue(nm, out var providedVar))
      {
        providedVar = ProxyProvidedVarWithCache.Create(
          RdProvidedTypeProcessModel.AsProvidedVar.Sync(new AsProvidedVarArgs(EntityId, nm)), myProcessModel, Context,
          myCache);

        myTypeAsVarsCache.Add(nm, providedVar);
      }

      return providedVar;
    }

    public override ProvidedTypeContext Context => myContext;

    private int? myArrayRank;
    private int? myMakeArrayTypeId;
    private int? myMakePointerTypeId;
    private int? myMakeByRefTypeId;
    private int? myBaseTypeId;
    private int? myGenericParameterPosition;
    private int? myGenericTypeDefinitionId;
    private int? myElementTypeId;
    private int? myEnumUnderlyingTypeId;
    private readonly Lazy<int?> myDeclaringTypeId;
    private readonly Lazy<ProvidedType[]> myInterfaces;
    private readonly Lazy<ProvidedMethodInfo[]> myMethods;
    private readonly Lazy<ProvidedType[]> myAllNestedTypes;
    private readonly Lazy<ProvidedType[]> myGenericArguments;
    private readonly Lazy<ProvidedPropertyInfo[]> myProperties;
    private readonly Lazy<ProvidedAssembly> myProvidedAssembly;
    private readonly Lazy<ProvidedParameterInfo[]> myStaticParameters;
    private readonly Lazy<ProvidedFieldInfo[]> myFields;
    private readonly Lazy<ProvidedEventInfo[]> myEvents;
    private readonly Lazy<ProvidedConstructorInfo[]> myConstructors;
    private readonly Dictionary<string, ProvidedVar> myTypeAsVarsCache;
    private readonly Dictionary<string, ProvidedType> myGeneratedTypesCache;
  }
}
