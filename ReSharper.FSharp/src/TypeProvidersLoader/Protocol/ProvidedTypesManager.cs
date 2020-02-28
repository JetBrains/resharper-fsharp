using System.Linq;
using FSharp.Compiler;
using JetBrains.Annotations;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using ProvidedType = FSharp.Compiler.ExtensionTyping.ProvidedType;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedTypesManager : IOutOfProcessProtocolManager<ProvidedType, RdProvidedType>
  {
    private readonly IOutOfProcessProtocolManager<ExtensionTyping.ProvidedPropertyInfo, RdProvidedPropertyInfo>
      myProvidedPropertiesManager;

    public ProvidedTypesManager(
      IOutOfProcessProtocolManager<ExtensionTyping.ProvidedPropertyInfo, RdProvidedPropertyInfo>
        providedPropertiesManager)
    {
      myProvidedPropertiesManager = providedPropertiesManager;
    }

    [ContractAnnotation("null => null")]
    public RdProvidedType Register(ProvidedType providedMethod)
    {
      if (providedMethod == null) return null;

      var providedTypeProtocolModel = new RdProvidedType(
        providedMethod.FullName,
        providedMethod.Namespace,
        providedMethod.IsGenericParameter,
        providedMethod.IsValueType,
        providedMethod.IsByRef,
        providedMethod.IsPointer,
        providedMethod.IsEnum,
        providedMethod.IsInterface,
        providedMethod.IsClass,
        providedMethod.IsSealed,
        providedMethod.IsAbstract,
        providedMethod.IsPublic,
        providedMethod.IsNestedPublic,
        providedMethod.IsSuppressRelocate,
        providedMethod.IsErased,
        providedMethod.IsGenericType,
        Register(providedMethod.BaseType),
        providedMethod.Name,
        Register(providedMethod.DeclaringType));

      providedTypeProtocolModel.GetInterfaces.Set((lifetime, _) => GetInterfaces(lifetime, providedMethod));
      providedTypeProtocolModel.GetNestedType.Set((lifetime, typeName) =>
        GetNestedType(lifetime, providedMethod, typeName));
      providedTypeProtocolModel.GetNestedTypes.Set((lifetime, _) => GetNestedTypes(lifetime, providedMethod));
      providedTypeProtocolModel.GetAllNestedTypes.Set((lifetime, _) => GetAllNestedTypes(lifetime, providedMethod));
      providedTypeProtocolModel.GetGenericTypeDefinition.Set((lifetime, _) =>
        GetGenericTypeDefinition(lifetime, providedMethod));
      providedTypeProtocolModel.GetElementType.Set((lifetime, _) => GetElementType(lifetime, providedMethod));
      providedTypeProtocolModel.GetGenericArguments.Set((lifetime, _) => GetGenericArguments(lifetime, providedMethod));
      providedTypeProtocolModel.GetArrayRank.Set((lifetime, _) => GetArrayRank(lifetime, providedMethod));
      providedTypeProtocolModel.GetEnumUnderlyingType.Set(
        (lifetime, _) => GetEnumUnderlyingType(lifetime, providedMethod));
      providedTypeProtocolModel.GetProperties.Set((lifetime, _) => GetProperties(lifetime, providedMethod));
      providedTypeProtocolModel.GetProperty.Set((lifetime, propName) => GetProperty(lifetime, providedMethod, propName));
      providedTypeProtocolModel.GenericParameterPosition.Set((lifetime, _) => GetGenericParameterPosition(lifetime, providedMethod));
      
      return providedTypeProtocolModel;
    }

    private RdTask<int> GetGenericParameterPosition(in Lifetime lifetime, ProvidedType providedType)
    {
      var genericParameterPosition = providedType.GetArrayRank();
      return RdTask<int>.Successful(genericParameterPosition);
    }

    private RdTask<RdProvidedPropertyInfo> GetProperty(in Lifetime lifetime, ProvidedType providedType, string propName)
    {
      var property = myProvidedPropertiesManager.Register(providedType.GetProperty(propName));
      return RdTask<RdProvidedPropertyInfo>.Successful(property);
    }

    private RdTask<RdProvidedPropertyInfo[]> GetProperties(in Lifetime lifetime, ProvidedType providedType)
    {
      var properties = providedType.GetProperties()
        .Select(myProvidedPropertiesManager.Register)
        .ToArray();
      return RdTask<RdProvidedPropertyInfo[]>.Successful(properties);
    }

    private RdTask<RdProvidedType> GetEnumUnderlyingType(in Lifetime lifetime, ProvidedType providedType)
    {
      var enumUnderlyingType = Register(providedType.GetEnumUnderlyingType());
      return RdTask<RdProvidedType>.Successful(enumUnderlyingType);
    }

    private RdTask<int> GetArrayRank(in Lifetime lifetime, ProvidedType providedType)
    {
      var arrayRank = providedType.GetArrayRank();
      return RdTask<int>.Successful(arrayRank);
    }

    private RdTask<RdProvidedType[]> GetGenericArguments(in Lifetime lifetime, ProvidedType providedType)
    {
      var genericArguments = providedType.GetGenericArguments().Select(Register).ToArray();
      return RdTask<RdProvidedType[]>.Successful(genericArguments);
    }

    private RdTask<RdProvidedType> GetElementType(in Lifetime lifetime, ProvidedType providedType)
    {
      var elementType = Register(providedType.GetElementType());
      return RdTask<RdProvidedType>.Successful(elementType);
    }

    private RdTask<RdProvidedType> GetGenericTypeDefinition(in Lifetime lifetime, ProvidedType providedType)
    {
      var genericTypeDefinition = Register(providedType.GetGenericTypeDefinition());
      return RdTask<RdProvidedType>.Successful(genericTypeDefinition);
    }

    private RdTask<RdProvidedType[]> GetAllNestedTypes(in Lifetime lifetime, ProvidedType providedType)
    {
      var nestedTypes = providedType.GetAllNestedTypes().Select(Register).ToArray();
      return RdTask<RdProvidedType[]>.Successful(nestedTypes);
    }

    private RdTask<RdProvidedType[]> GetNestedTypes(in Lifetime lifetime, ProvidedType providedType)
    {
      var nestedTypes = providedType.GetNestedTypes().Select(Register).ToArray();
      return RdTask<RdProvidedType[]>.Successful(nestedTypes);
    }

    private RdTask<RdProvidedType> GetNestedType(in Lifetime lifetime, ProvidedType providedType, string typeName)
    {
      var nestedType = Register(providedType.GetNestedType(typeName));
      return RdTask<RdProvidedType>.Successful(nestedType);
    }

    private RdTask<RdProvidedType[]> GetInterfaces(in Lifetime lifetime, ProvidedType providedType)
    {
      var interfaces = providedType.GetInterfaces().Select(Register).ToArray();
      return RdTask<RdProvidedType[]>.Successful(interfaces);
    }
  }
}
