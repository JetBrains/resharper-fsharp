using System.Collections.Generic;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedTypesManager : OutOfProcessProtocolManagerBase<ProvidedType, RdProvidedType>
  {
    private readonly IOutOfProcessProtocolManager<ProvidedParameterInfo, RdProvidedParameterInfo>
      myProvidedParameterInfosManager;

    private readonly IOutOfProcessProtocolManager<ProvidedMethodInfo, RdProvidedMethodInfo>
      myProvidedMethodInfosManager;

    private readonly IOutOfProcessProtocolManager<ProvidedPropertyInfo, RdProvidedPropertyInfo>
      myProvidedPropertiesManager;

    public ProvidedTypesManager() : base(new ProvidedTypeEqualityComparer())
    {
      myProvidedParameterInfosManager = new ProvidedParametersManager(this);
      myProvidedMethodInfosManager = new ProvidedMethodInfosManager(this, myProvidedParameterInfosManager);
      myProvidedPropertiesManager =
        new ProvidedPropertyInfoManager(myProvidedParameterInfosManager, this, myProvidedMethodInfosManager);
    }

    protected override RdProvidedType CreateProcessModel(
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var providedNativeModelProtocolModel = new RdProvidedType(
        providedNativeModel.FullName,
        providedNativeModel.Namespace,
        providedNativeModel.IsGenericParameter,
        providedNativeModel.IsValueType,
        providedNativeModel.IsByRef,
        providedNativeModel.IsPointer,
        providedNativeModel.IsArray,
        providedNativeModel.IsEnum,
        providedNativeModel.IsInterface,
        providedNativeModel.IsClass,
        providedNativeModel.IsSealed,
        providedNativeModel.IsAbstract,
        providedNativeModel.IsPublic,
        providedNativeModel.IsNestedPublic,
        providedNativeModel.IsSuppressRelocate,
        providedNativeModel.IsErased,
        providedNativeModel.IsGenericType,
        providedNativeModel.Name);

      providedNativeModelProtocolModel.BaseType.Set((lifetime, _) =>
        GetBaseType(lifetime, providedNativeModel, providedModelOwner));
      providedNativeModelProtocolModel.DeclaringType.Set((lifetime, _) =>
        GetDeclaringType(lifetime, providedNativeModel, providedModelOwner));
      providedNativeModelProtocolModel.GetInterfaces.Set((lifetime, _) =>
        GetInterfaces(lifetime, providedNativeModel, providedModelOwner));
      providedNativeModelProtocolModel.GetNestedType.Set((lifetime, typeName) =>
        GetNestedType(lifetime, providedNativeModel, providedModelOwner, typeName));
      providedNativeModelProtocolModel.GetNestedTypes.Set(
        (lifetime, _) => GetNestedTypes(lifetime, providedNativeModel, providedModelOwner));
      providedNativeModelProtocolModel.GetAllNestedTypes.Set((lifetime, _) =>
        GetAllNestedTypes(lifetime, providedNativeModel, providedModelOwner));
      providedNativeModelProtocolModel.GetGenericTypeDefinition.Set((lifetime, _) =>
        GetGenericTypeDefinition(lifetime, providedNativeModel, providedModelOwner));
      providedNativeModelProtocolModel.GetElementType.Set(
        (lifetime, _) => GetElementType(lifetime, providedNativeModel, providedModelOwner));
      providedNativeModelProtocolModel.GetGenericArguments.Set((lifetime, _) =>
        GetGenericArguments(lifetime, providedNativeModel, providedModelOwner));
      providedNativeModelProtocolModel.GetArrayRank.Set((lifetime, _) => GetArrayRank(lifetime, providedNativeModel));
      providedNativeModelProtocolModel.GetEnumUnderlyingType.Set(
        (lifetime, _) => GetEnumUnderlyingType(lifetime, providedNativeModel, providedModelOwner));
      providedNativeModelProtocolModel.GetProperties.Set((lifetime, _) =>
        GetProperties(lifetime, providedNativeModel, providedModelOwner));
      providedNativeModelProtocolModel.GetProperty.Set((lifetime, propName) =>
        GetProperty(lifetime, providedNativeModel, providedModelOwner, propName));
      providedNativeModelProtocolModel.GenericParameterPosition.Set((lifetime, _) =>
        GetGenericParameterPosition(lifetime, providedNativeModel));
      providedNativeModelProtocolModel.GetStaticParameters.Set((lifetime, _) =>
        GetStaticParameters(lifetime, providedNativeModel, providedModelOwner));
      providedNativeModelProtocolModel.GetMethods.Set((lifetime, _) =>
        GetMethods(lifetime, providedNativeModel, providedModelOwner));

      return providedNativeModelProtocolModel;
    }

    private RdTask<RdProvidedType> GetDeclaringType(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var declaringType = Register(providedNativeModel.DeclaringType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(declaringType);
    }

    private RdTask<RdProvidedType> GetBaseType(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var baseType = Register(providedNativeModel.BaseType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(baseType);
    }

    private RdTask<RdProvidedMethodInfo[]> GetMethods(in Lifetime lifetime, ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var interfaces = providedNativeModel
        .GetMethods()
        .Select(t => myProvidedMethodInfosManager.Register(t, providedModelOwner)).ToArray();
      return RdTask<RdProvidedMethodInfo[]>.Successful(interfaces);
    }

    private RdTask<RdProvidedParameterInfo[]> GetStaticParameters(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var staticParameters = providedNativeModel
        .GetStaticParameters(providedModelOwner)
        .Select(t => myProvidedParameterInfosManager.Register(t, providedModelOwner))
        .ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(staticParameters);
    }

    private RdTask<int> GetGenericParameterPosition(in Lifetime lifetime, ProvidedType providedNativeModel)
    {
      var genericParameterPosition = providedNativeModel.GetArrayRank();
      return RdTask<int>.Successful(genericParameterPosition);
    }

    private RdTask<RdProvidedPropertyInfo> GetProperty(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner,
      string propName)
    {
      var property =
        myProvidedPropertiesManager.Register(providedNativeModel.GetProperty(propName), providedModelOwner);
      return RdTask<RdProvidedPropertyInfo>.Successful(property);
    }

    private RdTask<RdProvidedPropertyInfo[]> GetProperties(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var properties = providedNativeModel.GetProperties()
        .Select(t => myProvidedPropertiesManager.Register(t, providedModelOwner))
        .ToArray();
      return RdTask<RdProvidedPropertyInfo[]>.Successful(properties);
    }

    private RdTask<RdProvidedType> GetEnumUnderlyingType(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var enumUnderlyingType = Register(providedNativeModel.GetEnumUnderlyingType(), providedModelOwner);
      return RdTask<RdProvidedType>.Successful(enumUnderlyingType);
    }

    private RdTask<int> GetArrayRank(
      in Lifetime lifetime,
      ProvidedType providedNativeModel)
    {
      var arrayRank = providedNativeModel.GetArrayRank();
      return RdTask<int>.Successful(arrayRank);
    }

    private RdTask<RdProvidedType[]> GetGenericArguments(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var genericArguments = providedNativeModel
        .GetGenericArguments()
        .Select(t => Register(t, providedModelOwner))
        .ToArray();
      return RdTask<RdProvidedType[]>.Successful(genericArguments);
    }

    private RdTask<RdProvidedType> GetElementType(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var elementType = Register(providedNativeModel.GetElementType(), providedModelOwner);
      return RdTask<RdProvidedType>.Successful(elementType);
    }

    private RdTask<RdProvidedType> GetGenericTypeDefinition(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var genericTypeDefinition = Register(providedNativeModel.GetGenericTypeDefinition(), providedModelOwner);
      return RdTask<RdProvidedType>.Successful(genericTypeDefinition);
    }

    private RdTask<RdProvidedType[]> GetAllNestedTypes(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var nestedTypes = providedNativeModel
        .GetAllNestedTypes()
        .Select(t => Register(t, providedModelOwner))
        .ToArray();
      return RdTask<RdProvidedType[]>.Successful(nestedTypes);
    }

    private RdTask<RdProvidedType[]> GetNestedTypes(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var nestedTypes = providedNativeModel
        .GetNestedTypes()
        .Select(t => Register(t, providedModelOwner))
        .ToArray();
      return RdTask<RdProvidedType[]>.Successful(nestedTypes);
    }

    private RdTask<RdProvidedType> GetNestedType(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner,
      string typeName)
    {
      var nestedType = Register(providedNativeModel.GetNestedType(typeName), providedModelOwner);
      return RdTask<RdProvidedType>.Successful(nestedType);
    }

    private RdTask<RdProvidedType[]> GetInterfaces(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var interfaces = providedNativeModel
        .GetInterfaces()
        .Select(t => Register(t, providedModelOwner)).ToArray();
      return RdTask<RdProvidedType[]>.Successful(interfaces);
    }
  }

  internal class ProvidedTypeEqualityComparer : IEqualityComparer<ProvidedType>
  {
    public bool Equals(ProvidedType x, ProvidedType y)
    {
      return ReferenceEquals(x, y);
    }

    public int GetHashCode(ProvidedType obj)
    {
      return obj.Name.GetHashCode();
    }
  }
}
