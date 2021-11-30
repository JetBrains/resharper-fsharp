using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
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
  public class ProxyProvidedType : ProvidedType, IRdProvidedEntity
  {
    private readonly RdOutOfProcessProvidedType myRdProvidedType;
    private readonly int myTypeProviderId;
    private readonly TypeProvidersContext myTypeProvidersContext;
    public int EntityId => myRdProvidedType.EntityId;
    public RdProvidedEntityType EntityType => RdProvidedEntityType.TypeInfo;

    private RdProvidedTypeProcessModel RdProvidedTypeProcessModel =>
      myTypeProvidersContext.Connection.ProtocolModel.RdProvidedTypeProcessModel;

    private ProxyProvidedType(RdOutOfProcessProvidedType rdProvidedType, int typeProviderId,
      TypeProvidersContext typeProvidersContext) : base(null, ProvidedConst.EmptyContext)
    {
      myRdProvidedType = rdProvidedType;
      myTypeProviderId = typeProviderId;
      myTypeProvidersContext = typeProvidersContext;

      myInterfaces = new InterruptibleLazy<ProvidedType[]>(() =>
        myTypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetInterfaces.Sync(EntityId)),
          typeProviderId));

      myGenericArguments = new InterruptibleLazy<ProvidedType[]>(() =>
        myTypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(myRdProvidedType.GenericArguments, typeProviderId));

      myMethods = new InterruptibleLazy<ProvidedMethodInfo[]>(() => myTypeProvidersContext.Connection
        .ExecuteWithCatch(() => RdProvidedTypeProcessModel.GetMethods.Sync(EntityId, RpcTimeouts.Maximal))
        .Select(t => ProxyProvidedMethodInfo.Create(t, typeProviderId, typeProvidersContext))
        .ToArray());

      myAllNestedTypes = new InterruptibleLazy<ProvidedType[]>(() =>
        myTypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetAllNestedTypes.Sync(EntityId, RpcTimeouts.Maximal)), typeProviderId));

      myProperties = new InterruptibleLazy<ProvidedPropertyInfo[]>(() => myTypeProvidersContext.Connection
        .ExecuteWithCatch(() => RdProvidedTypeProcessModel.GetProperties.Sync(EntityId, RpcTimeouts.Maximal))
        .Select(t => ProxyProvidedPropertyInfo.Create(t, myTypeProviderId, myTypeProvidersContext))
        .ToArray());

      myStaticParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(() => myTypeProvidersContext.Connection
        .ExecuteWithCatch(() => RdProvidedTypeProcessModel.GetStaticParameters.Sync(EntityId, RpcTimeouts.Maximal))
        .Select(t => ProxyProvidedParameterInfo.Create(t, myTypeProviderId, myTypeProvidersContext))
        .ToArray());

      myFields = new InterruptibleLazy<ProvidedFieldInfo[]>(() => myTypeProvidersContext.Connection
        .ExecuteWithCatch(() => RdProvidedTypeProcessModel.GetFields.Sync(EntityId, RpcTimeouts.Maximal))
        .Select(t => ProxyProvidedFieldInfo.Create(t, myTypeProviderId, typeProvidersContext))
        .ToArray());

      myEvents = new InterruptibleLazy<ProvidedEventInfo[]>(() => myTypeProvidersContext.Connection
        .ExecuteWithCatch(() => RdProvidedTypeProcessModel.GetEvents.Sync(EntityId, RpcTimeouts.Maximal))
        .Select(t => ProxyProvidedEventInfo.Create(t, myTypeProviderId, myTypeProvidersContext))
        .ToArray());

      myConstructors = new InterruptibleLazy<ProvidedConstructorInfo[]>(() => myTypeProvidersContext.Connection
        .ExecuteWithCatch(() => RdProvidedTypeProcessModel.GetConstructors.Sync(EntityId, RpcTimeouts.Maximal))
        .Select(t => ProxyProvidedConstructorInfo.Create(t, myTypeProviderId, myTypeProvidersContext))
        .ToArray());

      myCustomAttributes = new InterruptibleLazy<RdCustomAttributeData[]>(() =>
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetCustomAttributes(this));
    }

    [ContractAnnotation("type:null => null")]
    public static ProxyProvidedType Create(RdOutOfProcessProvidedType type, int typeProviderId,
      TypeProvidersContext typeProvidersContext) =>
      type == null ? null : new ProxyProvidedType(type, typeProviderId, typeProvidersContext);

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

    public override int GenericParameterPosition =>
      myGenericParameterPosition ??=
        myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
          RdProvidedTypeProcessModel.GenericParameterPosition.Sync(myRdProvidedType.EntityId));

    public override ProvidedType BaseType =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(myRdProvidedType.BaseType, myTypeProviderId);

    public override ProvidedType DeclaringType =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(myRdProvidedType.DeclaringType, myTypeProviderId);

    public override ProvidedType GetNestedType(string nm)
    {
      foreach (var t in myAllNestedTypes.Value)
        if (t.Name == nm)
          return t;

      return null;
    }

    public override ProvidedType[] GetNestedTypes() =>
      myAllNestedTypes.Value.Where(t => t.IsPublic || t.IsNestedPublic).ToArray();

    public override ProvidedType[] GetAllNestedTypes() => myAllNestedTypes.Value;

    public override ProvidedType GetGenericTypeDefinition() =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myGenericTypeDefinitionId ??=
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetGenericTypeDefinition.Sync(EntityId)),
        myTypeProviderId);

    public override ProvidedPropertyInfo[] GetProperties() => myProperties.Value;

    public override ProvidedPropertyInfo GetProperty(string nm)
    {
      foreach (var t in myProperties.Value)
        if (t.Name == nm)
          return t;

      return null;
    }

    public override int GetArrayRank() =>
      myArrayRank ??=
        myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
          RdProvidedTypeProcessModel.GetArrayRank.Sync(EntityId));

    public override ProvidedType GetElementType() =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myElementTypeId ??=
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetElementType.Sync(EntityId)),
        myTypeProviderId);

    public override ProvidedType[] GetGenericArguments() => myGenericArguments.Value;

    public override ProvidedType GetEnumUnderlyingType() =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myEnumUnderlyingTypeId ??=
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetEnumUnderlyingType.Sync(EntityId)),
        myTypeProviderId);

    public override ProvidedParameterInfo[] GetStaticParameters(ITypeProvider provider) => myStaticParameters.Value;

    public override ProvidedType ApplyStaticArguments(ITypeProvider provider, string[] fullTypePathAfterArguments,
      object[] staticArgs) => IsErased
      ? ApplyStaticArguments(fullTypePathAfterArguments, staticArgs)
      : ApplyStaticArgumentsGenerative(fullTypePathAfterArguments, staticArgs);

    private ProvidedType ApplyStaticArguments(string[] fullTypePathAfterArguments, object[] staticArgs)
    {
      var staticArgDescriptions = staticArgs.Select(PrimitiveTypesBoxer.BoxToServerStaticArg).ToArray();
      return myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myTypeProvidersContext.Connection.ExecuteWithCatch(
          () => RdProvidedTypeProcessModel.ApplyStaticArguments.Sync(
            new ApplyStaticArgumentsParameters(EntityId, fullTypePathAfterArguments, staticArgDescriptions),
            RpcTimeouts.Maximal)),
        myTypeProviderId);
    }

    // Since we distinguish different generative types by assembly name
    // and, at the same time, even for the same types, the generated assembly names will be different,
    // we will cache such types on the ReSharper side to avoid leaks.
    private ProvidedType ApplyStaticArgumentsGenerative(string[] fullTypePathAfterArguments, object[] staticArgs)
    {
      var key = string.Join(".", fullTypePathAfterArguments) + "+" + string.Join(",", staticArgs);
      var staticArgDescriptions = staticArgs.Select(PrimitiveTypesBoxer.BoxToServerStaticArg).ToArray();

      return myTypeProvidersContext.AppliedProvidedTypesCache.GetOrCreate((EntityId, key), myTypeProviderId,
        new ApplyStaticArgumentsParameters(EntityId, fullTypePathAfterArguments, staticArgDescriptions));
    }

    public override ProvidedType[] GetInterfaces() => myInterfaces.Value;

    public override ProvidedMethodInfo[] GetMethods() => myMethods.Value;

    public override ProvidedType MakeArrayType() => MakeArrayType(1);

    public override ProvidedType MakeArrayType(int rank) =>
      myTypeProvidersContext.ArrayProvidedTypesCache.GetOrCreate((EntityId, rank), myTypeProviderId,
        new MakeArrayTypeArgs(EntityId, rank));

    public override ProvidedType MakeGenericType(ProvidedType[] args)
    {
      var key = string.Join(",", args.Select(t => $"{t.Assembly.FullName} {t.FullName}"));

      var argIds = args
        .Cast<IRdProvidedEntity>()
        .Select(t => t.EntityId)
        .ToArray();

      return myTypeProvidersContext.GenericProvidedTypesCache.GetOrCreate((EntityId, key), myTypeProviderId,
        new MakeGenericTypeArgs(EntityId, argIds));
    }

    public override ProvidedType MakePointerType() =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myMakePointerTypeId ??=
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.MakePointerType.Sync(EntityId)),
        myTypeProviderId);

    public override ProvidedType MakeByRefType() =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myMakeByRefTypeId ??=
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.MakeByRefType.Sync(EntityId)),
        myTypeProviderId);

    public override ProvidedEventInfo[] GetEvents() => myEvents.Value;

    public override ProvidedEventInfo GetEvent(string nm)
    {
      foreach (var t in myEvents.Value)
        if (t.Name == nm)
          return t;

      return null;
    }

    public override ProvidedFieldInfo[] GetFields() => myFields.Value;

    public override ProvidedFieldInfo GetField(string nm)
    {
      foreach (var t in myFields.Value)
        if (t.Name == nm)
          return t;

      return null;
    }

    public override ProvidedConstructorInfo[] GetConstructors() => myConstructors.Value;

    public override ProvidedType ApplyContext(ProvidedTypeContext context) =>
      ProxyProvidedTypeWithContext.Create(this, context);

    public override ProvidedAssembly Assembly => myProvidedAssembly ??=
      myTypeProvidersContext.ProvidedAssembliesCache.GetOrCreate(myRdProvidedType.Assembly, myTypeProviderId);

    public override ProvidedVar AsProvidedVar(string nm) =>
      ProxyProvidedVar.Create(nm, false, this);

    public override
      FSharpOption<Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider _, string attribName) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetAttributeConstructorArgs(myCustomAttributes.Value,
        attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetDefinitionLocationAttribute(myCustomAttributes.Value);

    public override string[] GetXmlDocAttributes(ITypeProvider _) =>
      myXmlDocs ??=
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetXmlDocAttributes(myCustomAttributes.Value);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetHasTypeProviderEditorHideMethodsAttribute(
        myCustomAttributes.Value);

    private bool HasFlag(RdProvidedTypeFlags flag) => (myRdProvidedType.Flags & flag) == flag;

    private int? myArrayRank;
    private string[] myXmlDocs;
    private int? myMakePointerTypeId;
    private int? myMakeByRefTypeId;
    private int? myGenericParameterPosition;
    private int? myGenericTypeDefinitionId;
    private int? myElementTypeId;
    private int? myEnumUnderlyingTypeId;
    private ProvidedAssembly myProvidedAssembly;
    private readonly InterruptibleLazy<ProvidedType[]> myInterfaces;
    private readonly InterruptibleLazy<ProvidedMethodInfo[]> myMethods;
    private readonly InterruptibleLazy<ProvidedType[]> myAllNestedTypes;
    private readonly InterruptibleLazy<ProvidedType[]> myGenericArguments;
    private readonly InterruptibleLazy<ProvidedPropertyInfo[]> myProperties;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myStaticParameters;
    private readonly InterruptibleLazy<ProvidedFieldInfo[]> myFields;
    private readonly InterruptibleLazy<ProvidedEventInfo[]> myEvents;
    private readonly InterruptibleLazy<ProvidedConstructorInfo[]> myConstructors;
    private readonly InterruptibleLazy<RdCustomAttributeData[]> myCustomAttributes;
  }
}
