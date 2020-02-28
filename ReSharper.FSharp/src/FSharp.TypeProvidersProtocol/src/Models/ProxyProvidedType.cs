﻿using System.Linq;
using FSharp.Compiler;
using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using ProvidedType = FSharp.Compiler.ExtensionTyping.ProvidedType;

namespace JetBrains.ReSharper.Plugins.FSharp.Models
{
  public class ProxyProvidedType : ProvidedType
  {
    private readonly RdProvidedType myRdProvidedType;

    internal ProxyProvidedType(RdProvidedType rdProvidedType) : base(typeof(string),
      ExtensionTyping.ProvidedTypeContext.Empty)
    {
      myRdProvidedType = rdProvidedType;
    }

    [ContractAnnotation("null => null")]
    public static ProxyProvidedType Create(RdProvidedType type)
    {
      return type == null ? null : new ProxyProvidedType(type);
    }

    public override string Name => myRdProvidedType.Name;
    public override string FullName => myRdProvidedType.FullName;
    public override string Namespace => myRdProvidedType.Namespace;
    public override bool IsGenericParameter => myRdProvidedType.IsGenericParameter;
    public override bool IsValueType => myRdProvidedType.IsValueType;
    public override bool IsByRef => myRdProvidedType.IsByRef;
    public override bool IsPointer => myRdProvidedType.IsPointer;
    public override bool IsPublic => myRdProvidedType.IsPublic;
    public override bool IsNestedPublic => myRdProvidedType.IsNestedPublic;
    public override bool IsEnum => myRdProvidedType.IsEnum;
    public override bool IsClass => myRdProvidedType.IsClass;
    public override bool IsSealed => myRdProvidedType.IsSealed;
    public override bool IsAbstract => myRdProvidedType.IsAbstract;
    public override bool IsInterface => myRdProvidedType.IsInterface;
    public override bool IsSuppressRelocate => myRdProvidedType.IsSuppressRelocate;
    public override bool IsErased => myRdProvidedType.IsErased;
    public override bool IsGenericType => myRdProvidedType.IsGenericType;
    public override int GenericParameterPosition => myRdProvidedType.GenericParameterPosition.Sync(Core.Unit.Instance);
    public override ProvidedType BaseType => Create(myRdProvidedType.BaseType);
    public override ProvidedType DeclaringType => Create(myRdProvidedType.DeclaringType);
    public override ProvidedType GetNestedType(string nm) => Create(myRdProvidedType.GetNestedType.Sync(nm));

    public override ProvidedType[] GetNestedTypes()
    {
      return myRdProvidedType.GetNestedTypes
        .Sync(Core.Unit.Instance)
        .Select(Create)
        .ToArray();
    }

    public override ProvidedType[] GetAllNestedTypes()
    {
      return myRdProvidedType.GetAllNestedTypes
        .Sync(Core.Unit.Instance)
        .Select(Create)
        .ToArray();
    }

    public override ProvidedType GetGenericTypeDefinition()
    {
      return Create(myRdProvidedType.GetGenericTypeDefinition.Sync(Core.Unit.Instance));
    }

    public override ExtensionTyping.ProvidedPropertyInfo[] GetProperties()
    {
      return myRdProvidedType.GetProperties
        .Sync(Core.Unit.Instance)
        .Select(ProxyProvidedPropertyInfo.Create)
        .ToArray();
    }

    public override ExtensionTyping.ProvidedPropertyInfo GetProperty(string nm)
    {
      return ProxyProvidedPropertyInfo.Create(myRdProvidedType.GetProperty.Sync(nm));
    }

    public override int GetArrayRank() => myRdProvidedType.GetArrayRank.Sync(Core.Unit.Instance);

    public override ProvidedType GetElementType() => Create(myRdProvidedType.GetElementType.Sync(Core.Unit.Instance));

    public override ProvidedType[] GetGenericArguments()
    {
      return myRdProvidedType.GetGenericArguments
        .Sync(Core.Unit.Instance)
        .Select(Create)
        .ToArray();
    }

    public override ProvidedType GetEnumUnderlyingType() =>
      Create(myRdProvidedType.GetEnumUnderlyingType.Sync(Core.Unit.Instance));
  }
}
