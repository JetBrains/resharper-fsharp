using System.Linq;
using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedType : ProvidedType
  {
    private readonly RdProvidedType myRdProvidedType;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private int EntityId => myRdProvidedType.EntityId;
    private RdProvidedTypeProcessModel RdProvidedTypeProcessModel => myProcessModel.RdProvidedTypeProcessModel;

    internal ProxyProvidedType(RdProvidedType rdProvidedType,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context) : base(typeof(string), context)
    {
      myRdProvidedType = rdProvidedType;
      myProcessModel = processModel;
    }

    [ContractAnnotation("type:null => null")]
    public static ProxyProvidedType CreateNoContext(
      RdProvidedType type,
      RdFSharpTypeProvidersLoaderModel processModel) =>
      type == null ? null : new ProxyProvidedType(type, processModel, ProvidedTypeContext.Empty);

    [ContractAnnotation("type:null => null")]
    public static ProxyProvidedType Create(
      RdProvidedType type,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context) =>
      type == null ? null : new ProxyProvidedType(type, processModel, context);

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
      Create(RdProvidedTypeProcessModel.BaseType.Sync(EntityId), myProcessModel, Context);

    public override ProvidedType DeclaringType =>
      Create(RdProvidedTypeProcessModel.DeclaringType.Sync(EntityId), myProcessModel, Context);

    public override ProvidedType GetNestedType(string nm) =>
      Create(
        RdProvidedTypeProcessModel.GetNestedType.Sync(new GetNestedTypeArgs(EntityId, nm)),
        myProcessModel,
        Context);

    public override ProvidedType[] GetNestedTypes() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetNestedTypes
        .Sync(EntityId)
        .Select(t => Create(t, myProcessModel, Context))
        .ToArray();

    public override ProvidedType[] GetAllNestedTypes() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetAllNestedTypes
        .Sync(EntityId)
        .Select(t => Create(t, myProcessModel, Context))
        .ToArray();

    public override ProvidedType GetGenericTypeDefinition() =>
      Create(
        RdProvidedTypeProcessModel.GetGenericTypeDefinition.Sync(EntityId),
        myProcessModel,
        Context);

    public override ProvidedPropertyInfo[] GetProperties() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetProperties
        .Sync(EntityId)
        .Select(t => ProxyProvidedPropertyInfo.Create(t, myProcessModel, Context))
        .ToArray();

    public override ProvidedPropertyInfo GetProperty(string nm) =>
      ProxyProvidedPropertyInfo.Create(
        RdProvidedTypeProcessModel.GetProperty.Sync(new GetPropertyArgs(EntityId, nm)),
        myProcessModel,
        Context);

    public override int GetArrayRank() => RdProvidedTypeProcessModel.GetArrayRank.Sync(EntityId);

    public override ProvidedType GetElementType() =>
      Create(RdProvidedTypeProcessModel.GetElementType.Sync(EntityId), myProcessModel, Context);

    public override ProvidedType[] GetGenericArguments() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetGenericArguments
        .Sync(EntityId)
        .Select(t => Create(t, myProcessModel, Context))
        .ToArray();

    public override ProvidedType GetEnumUnderlyingType() =>
      Create(RdProvidedTypeProcessModel.GetEnumUnderlyingType.Sync(EntityId), myProcessModel, Context);

    public override ProvidedParameterInfo[] GetStaticParameters(ITypeProvider provider) =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetStaticParameters
        .Sync(EntityId)
        .Select(t => ProxyProvidedParameterInfo.Create(t, myProcessModel, Context))
        .ToArray();

    public override ProvidedType[] GetInterfaces() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetInterfaces
        .Sync(EntityId)
        .Select(t => Create(t, myProcessModel, Context))
        .ToArray();

    public override ProvidedMethodInfo[] GetMethods() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedTypeProcessModel.GetMethods
        .Sync(EntityId)
        .Select(t => ProxyProvidedMethodInfo.Create(t, myProcessModel, Context))
        .ToArray();

    public override ProvidedType ApplyContext(ProvidedTypeContext context) =>
      new ProxyProvidedType(myRdProvidedType, myProcessModel, context);
  }
}
