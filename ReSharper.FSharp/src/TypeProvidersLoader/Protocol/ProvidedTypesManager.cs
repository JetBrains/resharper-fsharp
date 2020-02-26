using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using ProvidedType = FSharp.Compiler.ExtensionTyping.ProvidedType;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedTypesManager : IOutOfProcessProtocolManager<ProvidedType, RdProvidedType>
  {
    public RdProvidedType Register(ProvidedType providedType)
    {
      var providedTypeProtocolModel = new RdProvidedType(providedType.Name,
        providedType.FullName,
        providedType.Namespace,
        providedType.IsGenericParameter,
        providedType.IsValueType,
        providedType.IsByRef,
        providedType.IsPointer,
        providedType.IsEnum,
        providedType.IsInterface,
        providedType.IsClass,
        providedType.IsSealed,
        providedType.IsAbstract,
        providedType.IsPublic,
        providedType.IsNestedPublic,
        providedType.IsSuppressRelocate,
        providedType.IsErased,
        providedType.IsGenericType,
        Register(providedType.BaseType));

      providedTypeProtocolModel.GetInterfaces.Set((lifetime, _) => GetInterfaces(lifetime, providedType));
      providedTypeProtocolModel.GetNestedType.Set((lifetime, typeName) =>
        GetNestedType(lifetime, providedType, typeName));
      providedTypeProtocolModel.GetNestedTypes.Set((lifetime, _) => GetNestedTypes(lifetime, providedType));
      providedTypeProtocolModel.GetAllNestedTypes.Set((lifetime, _) => GetAllNestedTypes(lifetime, providedType));
      providedTypeProtocolModel.GetGenericTypeDefinition.Set((lifetime, _) => GetGenericTypeDefinition(lifetime, providedType));

      return providedTypeProtocolModel;
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
