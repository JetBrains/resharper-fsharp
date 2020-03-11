using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedTypesHost : OutOfProcessProtocolHostBase<ProvidedType, RdProvidedType>
  {
    private readonly ITypeProvider myTypeProvider;

    private readonly IOutOfProcessProtocolHost<ProvidedParameterInfo, RdProvidedParameterInfo>
      myProvidedParameterInfosHost;

    private readonly IOutOfProcessProtocolHost<ProvidedMethodInfo, RdProvidedMethodInfo>
      myProvidedMethodInfosHost;

    private readonly IOutOfProcessProtocolHost<ProvidedPropertyInfo, RdProvidedPropertyInfo>
      myProvidedPropertiesHost;

    private readonly RdProvidedTypeProcessModel myRdProvidedTypeProcessModel;

    public ProvidedTypesHost(ITypeProvider typeProvider) : base(new ProvidedTypeEqualityComparer())
    {
      myTypeProvider = typeProvider;
      myProvidedParameterInfosHost = new ProvidedParametersHost(this);
      myProvidedMethodInfosHost = new ProvidedMethodInfosHost(this, myProvidedParameterInfosHost);
      myProvidedPropertiesHost =
        new ProvidedPropertyInfoHost(myProvidedParameterInfosHost, this, myProvidedMethodInfosHost);

      myRdProvidedTypeProcessModel = new RdProvidedTypeProcessModel();
      myRdProvidedTypeProcessModel.BaseType.Set((lifetime, _) =>
        GetBaseType(lifetime, providedNativeModel));
      myRdProvidedTypeProcessModel.DeclaringType.Set((lifetime, _) =>
        GetDeclaringType(lifetime, providedNativeModel));
      myRdProvidedTypeProcessModel.GetInterfaces.Set((lifetime, _) =>
        GetInterfaces(lifetime, providedNativeModel));
      myRdProvidedTypeProcessModel.GetNestedType.Set((lifetime, typeName) =>
        GetNestedType(lifetime, providedNativeModel, providedModelOwner, typeName));
      myRdProvidedTypeProcessModel.GetNestedTypes.Set(
        (lifetime, _) => GetNestedTypes(lifetime, providedNativeModel, providedModelOwner));
      myRdProvidedTypeProcessModel.GetAllNestedTypes.Set((lifetime, _) =>
        GetAllNestedTypes(lifetime, providedNativeModel, providedModelOwner));
      myRdProvidedTypeProcessModel.GetGenericTypeDefinition.Set((lifetime, _) =>
        GetGenericTypeDefinition(lifetime, providedNativeModel, providedModelOwner));
      myRdProvidedTypeProcessModel.GetElementType.Set(
        (lifetime, _) => GetElementType(lifetime, providedNativeModel, providedModelOwner));
      myRdProvidedTypeProcessModel.GetGenericArguments.Set((lifetime, _) =>
        GetGenericArguments(lifetime, providedNativeModel, providedModelOwner));
      myRdProvidedTypeProcessModel.GetArrayRank.Set((lifetime, _) => GetArrayRank(lifetime, providedNativeModel));
      myRdProvidedTypeProcessModel.GetEnumUnderlyingType.Set(
        (lifetime, _) => GetEnumUnderlyingType(lifetime, providedNativeModel, providedModelOwner));
      myRdProvidedTypeProcessModel.GetProperties.Set((lifetime, _) =>
        GetProperties(lifetime, providedNativeModel, providedModelOwner));
      myRdProvidedTypeProcessModel.GetProperty.Set((lifetime, propName) =>
        GetProperty(lifetime, providedNativeModel, providedModelOwner, propName));
      myRdProvidedTypeProcessModel.GenericParameterPosition.Set((lifetime, _) =>
        GetGenericParameterPosition(lifetime, providedNativeModel));
      myRdProvidedTypeProcessModel.GetStaticParameters.Set((lifetime, _) =>
        GetStaticParameters(lifetime, providedNativeModel, providedModelOwner));
      myRdProvidedTypeProcessModel.GetMethods.Set((lifetime, _) =>
        GetMethods(lifetime, providedNativeModel));
    }

    protected override RdProvidedType CreateRdModel(ProvidedType providedNativeModel, int entityId, int typeProviderId)
      => new RdProvidedType(providedNativeModel.FullName, providedNativeModel.Namespace,
        providedNativeModel.IsGenericParameter, providedNativeModel.IsValueType, providedNativeModel.IsByRef,
        providedNativeModel.IsPointer, providedNativeModel.IsEnum, providedNativeModel.IsArray,
        providedNativeModel.IsInterface, providedNativeModel.IsClass, providedNativeModel.IsSealed,
        providedNativeModel.IsAbstract, providedNativeModel.IsPublic, providedNativeModel.IsNestedPublic,
        providedNativeModel.IsSuppressRelocate, providedNativeModel.IsErased, providedNativeModel.IsGenericType,
        providedNativeModel.Name, entityId, typeProviderId);

    private RdTask<RdProvidedType> GetDeclaringType(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var declaringType = GetRdModel(providedNativeModel.DeclaringType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(declaringType);
    }

    private RdTask<RdProvidedType> GetBaseType(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var baseType = GetRdModel(providedNativeModel.BaseType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(baseType);
    }

    private RdTask<RdProvidedMethodInfo[]> GetMethods(in Lifetime lifetime, ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var interfaces = providedNativeModel
        .GetMethods()
        .Select(t => myProvidedMethodInfosHost.GetRdModel(t, providedModelOwner)).ToArray();
      return RdTask<RdProvidedMethodInfo[]>.Successful(interfaces);
    }

    private RdTask<RdProvidedParameterInfo[]> GetStaticParameters(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var staticParameters = providedNativeModel
        .GetStaticParameters(providedModelOwner)
        .Select(t => myProvidedParameterInfosHost.GetRdModel(t, providedModelOwner))
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
        myProvidedPropertiesHost.GetRdModel(providedNativeModel.GetProperty(propName), providedModelOwner);
      return RdTask<RdProvidedPropertyInfo>.Successful(property);
    }

    private RdTask<RdProvidedPropertyInfo[]> GetProperties(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var properties = providedNativeModel.GetProperties()
        .Select(t => myProvidedPropertiesHost.GetRdModel(t, providedModelOwner))
        .ToArray();
      return RdTask<RdProvidedPropertyInfo[]>.Successful(properties);
    }

    private RdTask<RdProvidedType> GetEnumUnderlyingType(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var enumUnderlyingType = GetRdModel(providedNativeModel.GetEnumUnderlyingType(), providedModelOwner);
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
        .Select(t => GetRdModel(t, providedModelOwner))
        .ToArray();
      return RdTask<RdProvidedType[]>.Successful(genericArguments);
    }

    private RdTask<RdProvidedType> GetElementType(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var elementType = GetRdModel(providedNativeModel.GetElementType(), providedModelOwner);
      return RdTask<RdProvidedType>.Successful(elementType);
    }

    private RdTask<RdProvidedType> GetGenericTypeDefinition(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var genericTypeDefinition = GetRdModel(providedNativeModel.GetGenericTypeDefinition(), providedModelOwner);
      return RdTask<RdProvidedType>.Successful(genericTypeDefinition);
    }

    private RdTask<RdProvidedType[]> GetAllNestedTypes(
      in Lifetime lifetime,
      ProvidedType providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var nestedTypes = providedNativeModel
        .GetAllNestedTypes()
        .Select(t => GetRdModel(t, providedModelOwner))
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
        .Select(t => GetRdModel(t, providedModelOwner))
        .ToArray();
      return RdTask<RdProvidedType[]>.Successful(nestedTypes);
    }

    private RdTask<RdProvidedType> GetNestedType(in Lifetime lifetime, int entityId, string typeName)
    {
      var providedType = GetEntity(entityId);

      var nestedType = GetRdModel(providedType.GetNestedType(typeName));
      return RdTask<RdProvidedType>.Successful(nestedType);
    }

    private RdTask<RdProvidedType[]> GetInterfaces(in Lifetime lifetime, int entityId)
    {
      var providedType = GetEntity(entityId);

      var interfaces = providedType
        .GetInterfaces()
        .Select(GetRdModel)
        .ToArray();
      return RdTask<RdProvidedType[]>.Successful(interfaces);
    }
  }
}
