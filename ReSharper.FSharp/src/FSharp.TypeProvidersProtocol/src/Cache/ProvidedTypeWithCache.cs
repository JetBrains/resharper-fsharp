﻿using System;
using System.Linq;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
{
  public class ProvidedTypeWithCache : ProvidedType
  {
    private readonly ProvidedType myProvidedType;

    public ProvidedTypeWithCache(ProvidedType providedType) : base(null, providedType.Context)
    {
      myProvidedType = providedType;

      myBaseType = new Lazy<ProvidedType>(() => myProvidedType.BaseType);
      myElementType = new Lazy<ProvidedType>(() => myProvidedType.GetElementType());
      myDeclaringType = new Lazy<ProvidedType>(() => myProvidedType.DeclaringType);
      myInterfaces = new Lazy<ProvidedType[]>(() => myProvidedType.GetInterfaces());
      myEnumUnderlyingType = new Lazy<ProvidedType>(() => myProvidedType.GetEnumUnderlyingType());
      myGenericArguments = new Lazy<ProvidedType[]>(() => myProvidedType.GetGenericArguments());
      myGenericTypeDefinition = new Lazy<ProvidedType>(() => myProvidedType.GetGenericTypeDefinition());
      myMethods = new Lazy<ProvidedMethodInfo[]>(() => myProvidedType.GetMethods());
      myAllNestedTypes = new Lazy<ProvidedType[]>(() => myProvidedType.GetAllNestedTypes());
      myProperties = new Lazy<ProvidedPropertyInfo[]>(() => myProvidedType.GetProperties());
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
    public override int GenericParameterPosition => myProvidedType.GenericParameterPosition;
    public override ProvidedType BaseType => myBaseType.Value;
    public override ProvidedType DeclaringType => myDeclaringType.Value;

    public override ProvidedType GetNestedType(string nm)
    {
      var types = GetAllNestedTypes();
      return types.First(t => t.Name == nm); //TODO: Optimize
    }

    public override ProvidedType[] GetNestedTypes()
    {
      var types = GetAllNestedTypes();
      return types.Where(t => t.IsPublic).ToArray(); //TODO: Optimize
    }

    public override ProvidedType[] GetAllNestedTypes() => myAllNestedTypes.Value;

    public override ProvidedType GetGenericTypeDefinition() => myGenericTypeDefinition.Value;


    public override ProvidedPropertyInfo[] GetProperties() => myProperties.Value;

    public override ProvidedPropertyInfo GetProperty(string nm)
    {
      var properties = GetProperties();
      return properties.First(t => t.Name == nm);
    }

    public override int GetArrayRank() =>
      myArrayRank ?? (myArrayRank = myProvidedType.GetArrayRank()).Value;

    public override ProvidedType GetElementType() => myElementType.Value;

    public override ProvidedType[] GetGenericArguments() => myGenericArguments.Value;

    public override ProvidedType GetEnumUnderlyingType() => myEnumUnderlyingType.Value;

    public override ProvidedParameterInfo[] GetStaticParameters(ITypeProvider provider) =>
      myStaticParameters ?? (myStaticParameters = myProvidedType.GetStaticParameters(provider));

    public override ProvidedType[] GetInterfaces() => myInterfaces.Value;

    public override ProvidedMethodInfo[] GetMethods() => myMethods.Value;

    public override ProvidedType ApplyContext(ProvidedTypeContext ctxt) => myProvidedType.ApplyContext(ctxt);

    private int? myArrayRank;
    private readonly Lazy<ProvidedType> myBaseType;
    private readonly Lazy<ProvidedType> myElementType;
    private ProvidedParameterInfo[] myStaticParameters;
    private readonly Lazy<ProvidedType[]> myInterfaces;
    private readonly Lazy<ProvidedType> myDeclaringType;
    private readonly Lazy<ProvidedMethodInfo[]> myMethods;
    private readonly Lazy<ProvidedType[]> myAllNestedTypes;
    private readonly Lazy<ProvidedType> myEnumUnderlyingType;
    private readonly Lazy<ProvidedType[]> myGenericArguments;
    private readonly Lazy<ProvidedPropertyInfo[]> myProperties;
    private readonly Lazy<ProvidedType> myGenericTypeDefinition;
  }
}
