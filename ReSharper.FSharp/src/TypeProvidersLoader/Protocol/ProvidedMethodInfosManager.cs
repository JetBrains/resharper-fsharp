using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedMethodInfosManager : IOutOfProcessProtocolManager<ProvidedMethodInfo, RdProvidedMethodInfo>
  {
    private static IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> myProvidedTypesManager =
      new ProvidedTypesManager(new ProvidedPropertyInfoManager());

    private IOutOfProcessProtocolManager<ProvidedParameterInfo, RdProvidedParameterInfo> myProvidedParameterInfosManager
      = new ProvidedParametersManager(myProvidedTypesManager);

    public RdProvidedMethodInfo Register(ProvidedMethodInfo providedMethod)
    {
      var methodInfoModel = new RdProvidedMethodInfo(
        myProvidedTypesManager.Register(providedMethod.ReturnType),
        providedMethod.MetadataToken,
        providedMethod.IsGenericMethod,
        providedMethod.IsStatic,
        providedMethod.IsFamily,
        providedMethod.IsFamilyAndAssembly,
        providedMethod.IsFamilyOrAssembly,
        providedMethod.IsVirtual,
        providedMethod.IsFinal,
        providedMethod.IsPublic,
        providedMethod.IsAbstract,
        providedMethod.IsHideBySig,
        providedMethod.IsConstructor,
        providedMethod.Name,
        myProvidedTypesManager.Register(providedMethod.DeclaringType));

      methodInfoModel.GetParameters.Set((lifetime, _) => GetParameters(lifetime, providedMethod));
      methodInfoModel.GetGenericArguments.Set((lifetime, _) => GetGenericArguments(lifetime, providedMethod));

      return methodInfoModel;
    }

    private RdTask<RdProvidedType[]> GetGenericArguments(in Lifetime lifetime, ProvidedMethodInfo providedMethod)
    {
      var genericArgs = providedMethod.GetGenericArguments().Select(myProvidedTypesManager.Register).ToArray();
      return RdTask<RdProvidedType[]>.Successful(genericArgs);
    }

    private RdTask<RdProvidedParameterInfo[]> GetParameters(in Lifetime lifetime, ProvidedMethodInfo providedMethod)
    {
      var parameters = providedMethod.GetParameters().Select(myProvidedParameterInfosManager.Register).ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(parameters);
    }
  }
}
