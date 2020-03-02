using System.Collections.Generic;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedMethodInfosManager : OutOfProcessProtocolManagerBase<ProvidedMethodInfo, RdProvidedMethodInfo>
  {
    private readonly IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> myProvidedTypesManager;

    private readonly IOutOfProcessProtocolManager<ProvidedParameterInfo, RdProvidedParameterInfo>
      myProvidedParameterInfosManager;

    public ProvidedMethodInfosManager(IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> providedTypesManager,
      IOutOfProcessProtocolManager<ProvidedParameterInfo, RdProvidedParameterInfo> providedParameterInfosManager) :
      base(new ProvidedMethodInfoEqualityComparer())
    {
      myProvidedTypesManager = providedTypesManager;
      myProvidedParameterInfosManager = providedParameterInfosManager;
    }

    protected override RdProvidedMethodInfo CreateProcessModel(
      ProvidedMethodInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var methodInfoModel = new RdProvidedMethodInfo(
        myProvidedTypesManager.Register(providedNativeModel.ReturnType, providedModelOwner),
        providedNativeModel.MetadataToken,
        providedNativeModel.IsGenericMethod,
        providedNativeModel.IsStatic,
        providedNativeModel.IsFamily,
        providedNativeModel.IsFamilyAndAssembly,
        providedNativeModel.IsFamilyOrAssembly,
        providedNativeModel.IsVirtual,
        providedNativeModel.IsFinal,
        providedNativeModel.IsPublic,
        providedNativeModel.IsAbstract,
        providedNativeModel.IsHideBySig,
        providedNativeModel.IsConstructor,
        providedNativeModel.Name,
        myProvidedTypesManager.Register(providedNativeModel.DeclaringType, providedModelOwner));

      methodInfoModel.GetParameters.Set((lifetime, _) =>
        GetParameters(lifetime, providedNativeModel, providedModelOwner));
      methodInfoModel.GetGenericArguments.Set((lifetime, _) =>
        GetGenericArguments(lifetime, providedNativeModel, providedModelOwner));

      return methodInfoModel;
    }

    private RdTask<RdProvidedType[]> GetGenericArguments(
      in Lifetime lifetime,
      ProvidedMethodInfo providedMethod,
      ITypeProvider providedModelOwner)
    {
      var genericArgs = providedMethod
        .GetGenericArguments()
        .Select(t => myProvidedTypesManager.Register(t, providedModelOwner)).ToArray();
      return RdTask<RdProvidedType[]>.Successful(genericArgs);
    }

    private RdTask<RdProvidedParameterInfo[]> GetParameters(
      in Lifetime lifetime,
      ProvidedMethodInfo providedMethod,
      ITypeProvider providedModelOwner)
    {
      var parameters = providedMethod
        .GetParameters()
        .Select(t => myProvidedParameterInfosManager.Register(t, providedModelOwner)).ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(parameters);
    }
  }

  internal class ProvidedMethodInfoEqualityComparer : IEqualityComparer<ProvidedMethodInfo>
  {
    public bool Equals(ProvidedMethodInfo x, ProvidedMethodInfo y)
    {
      return ReferenceEquals(x, y);
    }

    public int GetHashCode(ProvidedMethodInfo obj)
    {
      return obj.Name.GetHashCode();
    }
  }
}
