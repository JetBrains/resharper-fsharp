using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using JetBrains.Util.Concurrency;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
  public class ProxyProvidedType : ProvidedType, IProxyProvidedType
  {
    private record ProvidedTypeContent(
      ProxyProvidedType[] Interfaces,
      ProxyProvidedConstructorInfo[] Constructors,
      ProxyProvidedMethodInfo[] Methods,
      ProxyProvidedPropertyInfo[] Properties,
      ProxyProvidedFieldInfo[] Fields,
      ProxyProvidedEventInfo[] Events);

    private readonly RdOutOfProcessProvidedType myRdProvidedType;
    public TypeProvidersContext TypeProvidersContext { get; }
    public int EntityId => myRdProvidedType.EntityId;
    public RdProvidedEntityType EntityType => RdProvidedEntityType.TypeInfo;
    public RdCustomAttributeData[] Attributes => myCustomAttributes.Value;

    private RdProvidedTypeProcessModel RdProvidedTypeProcessModel =>
      TypeProvidersContext.Connection.ProtocolModel.RdProvidedTypeProcessModel;

    private ProxyProvidedType(RdOutOfProcessProvidedType rdProvidedType, IProxyTypeProvider typeProvider,
      TypeProvidersContext typeProvidersContext) : base(null, ProvidedConst.EmptyContext)
    {
      TypeProvider = typeProvider;
      myRdProvidedType = rdProvidedType;
      TypeProvidersContext = typeProvidersContext;

      myGenericArguments = new InterruptibleLazy<ProvidedType[]>(() =>
        TypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(myRdProvidedType.GenericArguments, TypeProvider));

      myStaticParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(() => typeProvidersContext.Connection
        .ExecuteWithCatch(() => RdProvidedTypeProcessModel.GetStaticParameters.Sync(EntityId, RpcTimeouts.Maximal))
        .Select(t => ProxyProvidedParameterInfo.Create(t, TypeProvider, TypeProvidersContext))
        .ToArray());

      myCustomAttributes = new InterruptibleLazy<RdCustomAttributeData[]>(() =>
        typeProvidersContext.ProvidedCustomAttributeProvider.GetCustomAttributes(this));

      myContent = new InterruptibleLazy<ProvidedTypeContent>(() =>
      {
        var rdProvidedTypeContent = typeProvidersContext.Connection.ExecuteWithCatch(() =>
          RdProvidedTypeProcessModel.GetContent.Sync(EntityId, RpcTimeouts.Maximal));

        var interfaces = TypeProvidersContext.ProvidedTypesCache
          .GetOrCreateBatch(rdProvidedTypeContent.Interfaces, TypeProvider);

        var constructors = rdProvidedTypeContent.Constructors
          .Select(t => ProxyProvidedConstructorInfo.Create(t, TypeProvider, typeProvidersContext))
          .ToArray();

        var methods = rdProvidedTypeContent.Methods
          .Select(t => ProxyProvidedMethodInfo.Create(t, TypeProvider, typeProvidersContext))
          .ToArray();

        var properties = rdProvidedTypeContent.Properties
          .Select(t => ProxyProvidedPropertyInfo.Create(t, TypeProvider, typeProvidersContext))
          .ToArray();

        var fields = rdProvidedTypeContent.Fields
          .Select(t => ProxyProvidedFieldInfo.Create(t, TypeProvider, typeProvidersContext))
          .ToArray();

        var events = rdProvidedTypeContent.Events
          .Select(t => ProxyProvidedEventInfo.Create(t, TypeProvider, typeProvidersContext))
          .ToArray();

        return new ProvidedTypeContent(interfaces, constructors, methods, properties, fields, events);
      });

      myAllNestedTypes = new InterruptibleLazy<ProxyProvidedType[]>(() =>
        typeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(
          typeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetAllNestedTypes.Sync(EntityId, RpcTimeouts.Maximal)), TypeProvider));
    }

    [ContractAnnotation("type:null => null")]
    public static ProxyProvidedType Create(RdOutOfProcessProvidedType type, IProxyTypeProvider typeProvider,
      TypeProvidersContext typeProvidersContext) =>
      type == null ? null : new ProxyProvidedType(type, typeProvider, typeProvidersContext);

    public string DisplayName => Name;
    public IProxyTypeProvider TypeProvider { get; }
    public override string Name => myRdProvidedType.Name;
    public override string FullName => myRdProvidedType.FullName;
    public override string Namespace => myRdProvidedType.Namespace;

    public override bool IsGenericParameter => HasFlag(RdProvidedTypeFlags.IsGenericParameter);
    public override bool IsValueType => HasFlag(RdProvidedTypeFlags.IsValueType);
    public override bool IsByRef => HasFlag(RdProvidedTypeFlags.IsByRef);
    public override bool IsPointer => HasFlag(RdProvidedTypeFlags.IsPointer);
    public override bool IsPublic => HasFlag(RdProvidedTypeFlags.IsPublic);
    public override bool IsNestedPublic => HasFlag(RdProvidedTypeFlags.IsNestedPublic);
    public override bool IsArray => HasFlag(RdProvidedTypeFlags.IsArray);
    public override bool IsEnum => HasFlag(RdProvidedTypeFlags.IsEnum);
    public override bool IsClass => HasFlag(RdProvidedTypeFlags.IsClass);
    public override bool IsSealed => HasFlag(RdProvidedTypeFlags.IsSealed);
    public override bool IsAbstract => HasFlag(RdProvidedTypeFlags.IsAbstract);
    public override bool IsInterface => HasFlag(RdProvidedTypeFlags.IsInterface);
    public override bool IsSuppressRelocate => HasFlag(RdProvidedTypeFlags.IsSuppressRelocate);
    public override bool IsErased => HasFlag(RdProvidedTypeFlags.IsErased);
    public override bool IsGenericType => HasFlag(RdProvidedTypeFlags.IsGenericType);
    public override bool IsVoid => HasFlag(RdProvidedTypeFlags.IsVoid);
    public override bool IsMeasure => HasFlag(RdProvidedTypeFlags.IsMeasure);
    public bool IsCreatedByProvider => HasFlag(RdProvidedTypeFlags.IsCreatedByProvider);

    public bool CanHaveProvidedTypeContext =>
      myCanHaveProvidedTypeContext ??= CanHaveProvidedTypeContextRec(this);

    public override int GenericParameterPosition =>
      myGenericParameterPosition ??=
        TypeProvidersContext.Connection.ExecuteWithCatch(() =>
          RdProvidedTypeProcessModel.GenericParameterPosition.Sync(myRdProvidedType.EntityId));

    public override ProvidedType BaseType =>
      TypeProvidersContext.ProvidedTypesCache.GetOrCreate(myRdProvidedType.BaseType, TypeProvider);

    public override ProvidedType DeclaringType =>
      TypeProvidersContext.ProvidedTypesCache.GetOrCreate(myRdProvidedType.DeclaringType, TypeProvider);

    public override ProvidedType GetNestedType(string nm) => GetAllNestedTypes().FirstOrDefault(t => t.Name == nm);

    public override ProvidedType[] GetNestedTypes() =>
      GetAllNestedTypes().Where(t => t.IsPublic || t.IsNestedPublic).ToArray();

    public override ProvidedType[] GetAllNestedTypes() => myAllNestedTypes.Value;

    public override ProvidedType GetGenericTypeDefinition() =>
      TypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myGenericTypeDefinitionId ??=
          TypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetGenericTypeDefinition.Sync(EntityId)),
        TypeProvider);

    public override ProvidedPropertyInfo[] GetProperties() => myContent.Value.Properties;

    public override ProvidedPropertyInfo GetProperty(string nm) => GetProperties().FirstOrDefault(t => t.Name == nm);

    public override int GetArrayRank() =>
      myArrayRank ??=
        TypeProvidersContext.Connection.ExecuteWithCatch(() =>
          RdProvidedTypeProcessModel.GetArrayRank.Sync(EntityId));

    public override ProvidedType GetElementType() =>
      TypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myElementTypeId ??=
          TypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetElementType.Sync(EntityId)),
        TypeProvider);

    public override ProvidedType[] GetGenericArguments() => myGenericArguments.Value;

    public override ProvidedType GetEnumUnderlyingType() =>
      TypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myEnumUnderlyingTypeId ??=
          TypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetEnumUnderlyingType.Sync(EntityId)),
        TypeProvider);

    public override ProvidedParameterInfo[] GetStaticParameters(ITypeProvider provider) => myStaticParameters.Value;

    public override ProvidedType ApplyStaticArguments(ITypeProvider provider, string[] fullTypePathAfterArguments,
      object[] staticArgs) => IsErased
      ? ApplyStaticArguments(fullTypePathAfterArguments, staticArgs)
      : ApplyStaticArgumentsGenerative(fullTypePathAfterArguments, staticArgs);

    private ProvidedType ApplyStaticArguments(string[] fullTypePathAfterArguments, object[] staticArgs)
    {
      var staticArgDescriptions = staticArgs.Select(PrimitiveTypesBoxer.BoxToServerStaticArg).ToArray();
      return TypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        TypeProvidersContext.Connection.ExecuteWithCatch(
          () => RdProvidedTypeProcessModel.ApplyStaticArguments.Sync(
            new ApplyStaticArgumentsParameters(EntityId, fullTypePathAfterArguments, staticArgDescriptions),
            RpcTimeouts.Maximal)),
        TypeProvider);
    }

    // Since we distinguish different generative types by assembly name
    // and, at the same time, even for the same types, the generated assembly names will be different,
    // we will cache such types on the ReSharper side to avoid leaks.
    private ProvidedType ApplyStaticArgumentsGenerative(string[] fullTypePathAfterArguments, object[] staticArgs)
    {
      var key = string.Join(".", fullTypePathAfterArguments) + "+" + string.Join(",", staticArgs);
      var staticArgDescriptions = staticArgs.Select(PrimitiveTypesBoxer.BoxToServerStaticArg).ToArray();

      var result = TypeProvidersContext.AppliedProvidedTypesCache.GetOrCreate((EntityId, key), TypeProvider,
        new ApplyStaticArgumentsParameters(EntityId, fullTypePathAfterArguments, staticArgDescriptions));

      return result;
    }

    public override ProvidedType[] GetInterfaces() => myContent.Value.Interfaces;

    public override ProvidedMethodInfo[] GetMethods() => myContent.Value.Methods;

    public override ProvidedType MakeArrayType() => MakeArrayType(1);

    public override ProvidedType MakeArrayType(int rank) =>
      TypeProvidersContext.ArrayProvidedTypesCache.GetOrCreate((EntityId, rank), TypeProvider,
        new MakeArrayTypeArgs(EntityId, rank));

    public override ProvidedType MakeGenericType(ProvidedType[] args)
    {
      var key = string.Join(",", args.Select(t => $"{t.Assembly.FullName} {t.FullName}"));

      var argIds = args
        .Cast<IRdProvidedEntity>()
        .Select(t => t.EntityId)
        .ToArray();

      return TypeProvidersContext.GenericProvidedTypesCache.GetOrCreate((EntityId, key), TypeProvider,
        new MakeGenericTypeArgs(EntityId, argIds));
    }

    public override ProvidedType MakePointerType() =>
      TypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myMakePointerTypeId ??=
          TypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.MakePointerType.Sync(EntityId)),
        TypeProvider);

    public override ProvidedType MakeByRefType() =>
      TypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myMakeByRefTypeId ??=
          TypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.MakeByRefType.Sync(EntityId)),
        TypeProvider);

    public override ProvidedEventInfo[] GetEvents() => myContent.Value.Events;

    public override ProvidedEventInfo GetEvent(string nm) => GetEvents().FirstOrDefault(t => t.Name == nm);

    public override ProvidedFieldInfo[] GetFields() => myContent.Value.Fields;

    public override ProvidedFieldInfo GetField(string nm) => GetFields().FirstOrDefault(t => t.Name == nm);

    public override ProvidedConstructorInfo[] GetConstructors() => myContent.Value.Constructors;

    public override ProvidedType ApplyContext(ProvidedTypeContext context) =>
      ProxyProvidedTypeWithContext.Create(this, context);

    public override ProvidedAssembly Assembly => myProvidedAssembly ??=
      TypeProvidersContext.ProvidedAssembliesCache.GetOrCreate(myRdProvidedType.Assembly, TypeProvider);

    public override ProvidedVar AsProvidedVar(string nm) =>
      ProxyProvidedVar.Create(nm, false, this);

    public override
      FSharpOption<Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider _, string attribName) =>
      TypeProvidersContext.ProvidedCustomAttributeProvider.GetAttributeConstructorArgs(myCustomAttributes.Value,
        attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider _) =>
      TypeProvidersContext.ProvidedCustomAttributeProvider.GetDefinitionLocationAttribute(myCustomAttributes.Value);

    public override string[] GetXmlDocAttributes(ITypeProvider _) =>
      myXmlDocs ??=
        TypeProvidersContext.ProvidedCustomAttributeProvider.GetXmlDocAttributes(myCustomAttributes.Value);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider _) =>
      TypeProvidersContext.ProvidedCustomAttributeProvider.GetHasTypeProviderEditorHideMethodsAttribute(
        myCustomAttributes.Value);

    public IClrTypeName GetClrName() => new ClrTypeName(FullName);

    public override bool Equals(object y) =>
      y switch
      {
        ProxyProvidedType x => x.EntityId == EntityId,
        _ => false
      };

    public override int GetHashCode() => EntityId.GetHashCode();

    private bool HasFlag(RdProvidedTypeFlags flag) => (myRdProvidedType.Flags & flag) == flag;

    private static bool CanHaveProvidedTypeContextRec(ProvidedType type)
    {
      if (type is IProxyProvidedType { IsCreatedByProvider: true }) return true;

      if (type.IsArray || type.IsByRef || type.IsPointer)
        return CanHaveProvidedTypeContextRec(type.GetElementType());

      if (type.IsGenericType)
        foreach (var arg in type.GetGenericArguments())
          if (CanHaveProvidedTypeContextRec(arg))
            return true;

      return false;
    }

    private int? myArrayRank;
    private string[] myXmlDocs;
    private int? myMakePointerTypeId;
    private int? myMakeByRefTypeId;
    private int? myGenericParameterPosition;
    private int? myGenericTypeDefinitionId;
    private int? myElementTypeId;
    private int? myEnumUnderlyingTypeId;
    private bool? myCanHaveProvidedTypeContext;
    private ProvidedAssembly myProvidedAssembly;
    private readonly InterruptibleLazy<ProvidedType[]> myGenericArguments;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myStaticParameters;
    private readonly InterruptibleLazy<ProvidedTypeContent> myContent;
    private readonly InterruptibleLazy<ProxyProvidedType[]> myAllNestedTypes;
    private readonly InterruptibleLazy<RdCustomAttributeData[]> myCustomAttributes;
  }
}
