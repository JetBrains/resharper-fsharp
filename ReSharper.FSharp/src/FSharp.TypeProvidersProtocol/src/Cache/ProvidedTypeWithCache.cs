using System;
using System.Linq;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
{
  public class ProvidedTypeWithCache : ProvidedType
  {
    private readonly ProvidedType myProvidedType;

    public ProvidedTypeWithCache(ProvidedType providedType) : base(providedType.RawSystemType, providedType.Context)
    {
      myProvidedType = providedType;

      myBaseType = new Lazy<ProvidedType>(() => myProvidedType.BaseType);
      myDeclaringType = new Lazy<ProvidedType>(() => myProvidedType.DeclaringType.WithCache());
      myInterfaces = new Lazy<ProvidedType[]>(() => myProvidedType.GetInterfaces().WithCache());
      myGenericArguments = new Lazy<ProvidedType[]>(() => myProvidedType.GetGenericArguments().WithCache());
      myMethods = new Lazy<ProvidedMethodInfo[]>(() => myProvidedType.GetMethods());
      myAllNestedTypes = new Lazy<ProvidedType[]>(() => myProvidedType.GetAllNestedTypes().WithCache());
      myProperties = new Lazy<ProvidedPropertyInfo[]>(() => myProvidedType.GetProperties());
      myProvidedAssembly = new Lazy<ProvidedAssembly>(() => myProvidedType.Assembly);
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
      return types.First(t => t.FullName == nm); //TODO: Optimize
    }

    public override ProvidedType[] GetNestedTypes()
    {
      var types = GetAllNestedTypes();
      return types.Where(t => t.IsPublic).ToArray(); //TODO: Optimize
    }

    public override ProvidedType[] GetAllNestedTypes() => myAllNestedTypes.Value;

    public override ProvidedPropertyInfo[] GetProperties() => myProperties.Value;

    public override ProvidedPropertyInfo GetProperty(string nm)
    {
      var properties = GetProperties();
      return properties.First(t => t.Name == nm);
    }

    public override int GetArrayRank() =>
      myArrayRank ?? (myArrayRank = myProvidedType.GetArrayRank()).Value;

    public override ProvidedType[] GetGenericArguments() => myGenericArguments.Value;

    public override ProvidedParameterInfo[] GetStaticParameters(ITypeProvider provider) =>
      myStaticParameters?.Value ?? (myStaticParameters = new Lazy<ProvidedParameterInfo[]>(
        () => myProvidedType.GetStaticParameters(provider))).Value;

    public override ProvidedType[] GetInterfaces() => myInterfaces.Value;

    public override ProvidedMethodInfo[] GetMethods() => myMethods.Value;

    public override ProvidedType ApplyContext(ProvidedTypeContext context) => myProvidedType.ApplyContext(context);

    public override ProvidedType ApplyStaticArguments(ITypeProvider provider, string[] fullTypePathAfterArguments,
      object[] staticArgs) => myProvidedType.ApplyStaticArguments(provider, fullTypePathAfterArguments, staticArgs);

    public override ProvidedAssembly Assembly => myProvidedAssembly.Value;

    private int? myArrayRank;
    private readonly Lazy<ProvidedType> myBaseType;
    private readonly Lazy<ProvidedType[]> myInterfaces;
    private readonly Lazy<ProvidedType> myDeclaringType;
    private readonly Lazy<ProvidedMethodInfo[]> myMethods;
    private readonly Lazy<ProvidedType[]> myAllNestedTypes;
    private readonly Lazy<ProvidedType[]> myGenericArguments;
    private Lazy<ProvidedParameterInfo[]> myStaticParameters;
    private readonly Lazy<ProvidedPropertyInfo[]> myProperties;
    private readonly Lazy<ProvidedAssembly> myProvidedAssembly;
  }
}
