using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
  public class ProxyProvidedTypeWithContext : ProvidedType, IProxyProvidedType
  {
    private readonly ProvidedType myProvidedType;
    private IClrTypeName myClrTypeName;
    private ProxyProvidedType ProxyProvidedType => (ProxyProvidedType)myProvidedType;

    private ProxyProvidedTypeWithContext(ProvidedType providedType, ProvidedTypeContext context) : base(null, context)
    {
      Assertion.Assert(providedType is ProxyProvidedType);
      myProvidedType = providedType;

      var type = context.TryGetILTypeRef(providedType);
      if (type != null && ProxyProvidedType.TypeProvider.PsiModule != null)
        ProxyProvidedType.TypeProvidersContext.ProvidedAbbreviations.AddOrUpdate(this);
    }

    [ContractAnnotation("type:null => null")]
    public static ProvidedType Create(ProvidedType type, ProvidedTypeContext context) => type switch
    {
      null => null,
      ProxyProvidedType { CanHaveProvidedTypeContext: true } => new ProxyProvidedTypeWithContext(type, context),
      _ => type
    };

    public static ProvidedType[] Create(ProvidedType[] propertyInfos, ProvidedTypeContext context)
    {
      var result = new ProvidedType[propertyInfos.Length];
      for (var i = 0; i < propertyInfos.Length; i++)
        result[i] = Create(propertyInfos[i], context);

      return result;
    }

    public override string Name => myProvidedType.Name;
    public override string FullName => myProvidedType.FullName;
    public override string Namespace => myProvidedType.Namespace;

    public override bool IsGenericParameter => myProvidedType.IsGenericParameter;
    public override bool IsValueType => myProvidedType.IsValueType;
    public override bool IsByRef => myProvidedType.IsByRef;
    public override bool IsPointer => myProvidedType.IsPointer;
    public override bool IsPublic => myProvidedType.IsPublic;
    public override bool IsNestedPublic => myProvidedType.IsNestedPublic;
    public override bool IsArray => myProvidedType.IsArray;
    public override bool IsEnum => myProvidedType.IsEnum;
    public override bool IsClass => myProvidedType.IsClass;
    public override bool IsSealed => myProvidedType.IsSealed;
    public override bool IsAbstract => myProvidedType.IsAbstract;
    public override bool IsInterface => myProvidedType.IsInterface;
    public override bool IsSuppressRelocate => myProvidedType.IsSuppressRelocate;
    public override bool IsErased => myProvidedType.IsErased;
    public override bool IsGenericType => myProvidedType.IsGenericType;
    public override bool IsVoid => myProvidedType.IsVoid;
    public override bool IsMeasure => myProvidedType.IsMeasure;

    public override int GenericParameterPosition => myProvidedType.GenericParameterPosition;

    public override ProvidedType BaseType =>
      Create(myProvidedType.BaseType, Context);

    public override ProvidedType DeclaringType =>
      Create(myProvidedType.DeclaringType, Context);

    public override ProvidedType GetNestedType(string nm) =>
      Create(myProvidedType.GetNestedType(nm), Context);

    public override ProvidedType[] GetNestedTypes() =>
      Create(myProvidedType.GetNestedTypes(), Context);

    public override ProvidedType[] GetAllNestedTypes() =>
      Create(myProvidedType.GetAllNestedTypes(), Context);

    public override ProvidedType GetGenericTypeDefinition() =>
      Create(myProvidedType.GetGenericTypeDefinition(), Context);

    public override ProvidedPropertyInfo[] GetProperties() =>
      ProxyProvidedPropertyInfoWithContext.Create(myProvidedType.GetProperties(), Context);

    public override ProvidedPropertyInfo GetProperty(string nm) =>
      ProxyProvidedPropertyInfoWithContext.Create(myProvidedType.GetProperty(nm), Context);

    public override int GetArrayRank() =>
      myProvidedType.GetArrayRank();

    public override ProvidedType GetElementType() =>
      Create(myProvidedType.GetElementType(), Context);

    public override ProvidedType[] GetGenericArguments() =>
      Create(myProvidedType.GetGenericArguments(), Context);

    public override ProvidedType GetEnumUnderlyingType() =>
      Create(myProvidedType.GetEnumUnderlyingType(), Context);

    public override ProvidedParameterInfo[] GetStaticParameters(ITypeProvider provider) =>
      ProxyProvidedParameterInfoWithContext.Create(myProvidedType.GetStaticParameters(provider), Context);

    public override ProvidedType ApplyStaticArguments(ITypeProvider provider, string[] fullTypePathAfterArguments,
      object[] staticArgs) =>
      Create(myProvidedType.ApplyStaticArguments(provider, fullTypePathAfterArguments, staticArgs), Context);

    public override ProvidedType[] GetInterfaces() =>
      Create(myProvidedType.GetInterfaces(), Context);

    public override ProvidedMethodInfo[] GetMethods() =>
      ProxyProvidedMethodInfoWithContext.Create(myProvidedType.GetMethods(), Context);

    public override ProvidedType MakeArrayType() => MakeArrayType(1);

    public override ProvidedType MakeArrayType(int rank) =>
      Create(myProvidedType.MakeArrayType(rank), Context);

    public override ProvidedType MakeGenericType(ProvidedType[] args) =>
      Create(myProvidedType.MakeGenericType(args), Context);

    public override ProvidedType MakePointerType() =>
      Create(myProvidedType.MakePointerType(), Context);

    public override ProvidedType MakeByRefType() =>
      Create(myProvidedType.MakeByRefType(), Context);

    public override ProvidedEventInfo[] GetEvents() =>
      ProxyProvidedEventInfoWithContext.Create(myProvidedType.GetEvents(), Context);

    public override ProvidedEventInfo GetEvent(string nm) =>
      ProxyProvidedEventInfoWithContext.Create(myProvidedType.GetEvent(nm), Context);

    public override ProvidedFieldInfo[] GetFields() =>
      ProxyProvidedFieldInfoWithContext.Create(myProvidedType.GetFields(), Context);

    public override ProvidedFieldInfo GetField(string nm) =>
      ProxyProvidedFieldInfoWithContext.Create(myProvidedType.GetField(nm), Context);

    public override ProvidedConstructorInfo[] GetConstructors() =>
      ProxyProvidedConstructorInfoWithContext.Create(myProvidedType.GetConstructors(), Context);

    public override ProvidedType ApplyContext(ProvidedTypeContext context) =>
      Create(this, context);

    public override ProvidedAssembly Assembly => myProvidedType.Assembly;

    public override ProvidedVar AsProvidedVar(string nm) =>
      ProxyProvidedVar.Create(nm, false, this);

    public override FSharpOption<
        Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider tp, string attribName) =>
      myProvidedType.GetAttributeConstructorArgs(tp, attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider tp) =>
      myProvidedType.GetDefinitionLocationAttribute(tp);

    public override string[] GetXmlDocAttributes(ITypeProvider tp) =>
      myProvidedType.GetXmlDocAttributes(tp);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider tp) =>
      myProvidedType.GetHasTypeProviderEditorHideMethodsAttribute(tp);

    public int EntityId => ProxyProvidedType.EntityId;
    public RdProvidedEntityType EntityType => ProxyProvidedType.EntityType;
    public RdCustomAttributeData[] Attributes => ProxyProvidedType.Attributes;

    public override bool Equals(object y) =>
      y switch
      {
        IProxyProvidedType x => x.EntityId == EntityId,
        _ => false
      };

    public override int GetHashCode() => EntityId.GetHashCode();

    public bool IsCreatedByProvider => ProxyProvidedType.IsCreatedByProvider;

    public IClrTypeName GetClrName() => myClrTypeName ??= ProxyProvidedType switch
    {
      IProxyProvidedType { IsCreatedByProvider: true } =>
        new ClrTypeName(Context.TryGetILTypeRef(this).Value.BasicQualifiedName),
      IProxyProvidedType type => type.GetClrName()
    };

    public string DisplayName => Context.TryGetILTypeRef(this).Value.Name;
    public IProxyTypeProvider TypeProvider => ProxyProvidedType.TypeProvider;
  }
}
