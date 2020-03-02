using System.Linq;
using JetBrains.Annotations;
using JetBrains.Core;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedType : ProvidedType
  {
    private readonly RdProvidedType myRdProvidedType;

    internal ProxyProvidedType(RdProvidedType rdProvidedType, ProvidedTypeContext ctxt) : base(typeof(string), ctxt)
    {
      myRdProvidedType = rdProvidedType;
    }

    [ContractAnnotation("null => null")]
    public static ProxyProvidedType CreateNoContext(RdProvidedType type) =>
      type == null ? null : new ProxyProvidedType(type, ProvidedTypeContext.Empty);
    
    [ContractAnnotation("type:null => null")]
    public static ProxyProvidedType Create(RdProvidedType type, ProvidedTypeContext ctxt) =>
      type == null ? null : new ProxyProvidedType(type, ctxt);

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
    public override int GenericParameterPosition => myRdProvidedType.GenericParameterPosition.Sync(Core.Unit.Instance);
    public override ProvidedType BaseType => Create(myRdProvidedType.BaseType.Sync(Unit.Instance), Context);
    public override ProvidedType DeclaringType => Create(myRdProvidedType.DeclaringType.Sync(Unit.Instance), Context);
    public override ProvidedType GetNestedType(string nm) => Create(myRdProvidedType.GetNestedType.Sync(nm), Context);

    public override ProvidedType[] GetNestedTypes()
    {
      return myRdProvidedType.GetNestedTypes
        .Sync(Core.Unit.Instance)
        .Select(t => Create(t, Context))
        .ToArray();
    }

    public override ProvidedType[] GetAllNestedTypes()
    {
      return myRdProvidedType.GetAllNestedTypes
        .Sync(Core.Unit.Instance)
        .Select(t => Create(t, Context))
        .ToArray();
    }

    public override ProvidedType GetGenericTypeDefinition()
    {
      return Create(myRdProvidedType.GetGenericTypeDefinition.Sync(Core.Unit.Instance), Context);
    }

    public override ProvidedPropertyInfo[] GetProperties()
    {
      return myRdProvidedType.GetProperties
        .Sync(Core.Unit.Instance, RpcTimeouts.Maximal)
        .Select(t => ProxyProvidedPropertyInfo.Create(t, Context))
        .ToArray();
    }

    public override ProvidedPropertyInfo GetProperty(string nm)
    {
      return ProxyProvidedPropertyInfo.Create(myRdProvidedType.GetProperty.Sync(nm), Context);
    }

    public override int GetArrayRank() => myRdProvidedType.GetArrayRank.Sync(Core.Unit.Instance);

    public override ProvidedType GetElementType() => Create(myRdProvidedType.GetElementType.Sync(Core.Unit.Instance), Context);

    public override ProvidedType[] GetGenericArguments()
    {
      return myRdProvidedType.GetGenericArguments
        .Sync(Core.Unit.Instance)
        .Select(t => Create(t, Context))
        .ToArray();
    }

    public override ProvidedType GetEnumUnderlyingType() =>
      Create(myRdProvidedType.GetEnumUnderlyingType.Sync(Core.Unit.Instance), Context);

    public override ProvidedParameterInfo[] GetStaticParameters(ITypeProvider provider)
    {
      return myRdProvidedType.GetStaticParameters
        .Sync(Core.Unit.Instance)
        .Select(t => ProxyProvidedParameterInfo.Create(t, Context))
        .ToArray();
    }

    public override ProvidedType[] GetInterfaces()
    {
      return myRdProvidedType.GetInterfaces
        .Sync(Core.Unit.Instance)
        .Select(t => Create(t, Context))
        .ToArray();
    }

    public override ProvidedMethodInfo[] GetMethods()
    {
      return myRdProvidedType.GetMethods
        .Sync(Core.Unit.Instance)
        .Select(t => ProxyProvidedMethodInfo.Create(t, Context))
        .ToArray();
    }

    public override ProvidedType ApplyContext(ProvidedTypeContext ctxt)
    {
      return new ProxyProvidedType(myRdProvidedType, ctxt);
    }
  }
}
