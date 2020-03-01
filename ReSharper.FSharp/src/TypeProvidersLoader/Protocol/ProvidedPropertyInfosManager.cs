using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class
    ProvidedPropertyInfoManager : OutOfProcessProtocolManagerBase<ProvidedPropertyInfo, RdProvidedPropertyInfo>
  {
    private readonly IOutOfProcessProtocolManager<ProvidedParameterInfo, RdProvidedParameterInfo>
      myProvidedParameterInfosManager;

    private readonly IOutOfProcessProtocolManager<ProvidedMethodInfo, RdProvidedMethodInfo>
      myProvidedMethodInfosManager;

    private readonly IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> myProvidedTypesManager;

    public ProvidedPropertyInfoManager(
      IOutOfProcessProtocolManager<ProvidedParameterInfo, RdProvidedParameterInfo>
        providedParameterInfosManager,
      IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> providedTypesManager)
    {
      myProvidedTypesManager = providedTypesManager;
      myProvidedParameterInfosManager = providedParameterInfosManager;
      myProvidedMethodInfosManager =
        new ProvidedMethodInfosManager(myProvidedTypesManager, myProvidedParameterInfosManager);
    }

    protected override RdProvidedPropertyInfo CreateProcessModel(
      ProvidedPropertyInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var ppModel = new RdProvidedPropertyInfo(providedNativeModel.CanRead,
        providedNativeModel.CanWrite,
        myProvidedTypesManager.Register(providedNativeModel.PropertyType, providedModelOwner),
        providedNativeModel.Name,
        myProvidedTypesManager.Register(providedNativeModel.DeclaringType, providedModelOwner));

      ppModel.GetGetMethod.Set((lifetime, _) => GetGetMethod(lifetime, providedNativeModel, providedModelOwner));
      ppModel.GetSetMethod.Set((lifetime, _) => GetSetMethod(lifetime, providedNativeModel, providedModelOwner));
      ppModel.GetIndexParameters.Set((lifetime, _) =>
        GetIndexParameters(lifetime, providedNativeModel, providedModelOwner));

      return ppModel;
    }

    private RdTask<RdProvidedParameterInfo[]> GetIndexParameters(
      in Lifetime lifetime,
      ProvidedPropertyInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var indexParameters = providedNativeModel
        .GetIndexParameters()
        .Select(t => myProvidedParameterInfosManager.Register(t, providedModelOwner))
        .ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(indexParameters);
    }

    private RdTask<RdProvidedMethodInfo> GetSetMethod(
      in Lifetime lifetime,
      ProvidedPropertyInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var setMethod = myProvidedMethodInfosManager.Register(providedNativeModel.GetSetMethod(), providedModelOwner);
      return RdTask<RdProvidedMethodInfo>.Successful(setMethod);
    }

    private RdTask<RdProvidedMethodInfo> GetGetMethod(
      in Lifetime lifetime,
      ProvidedPropertyInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var getMethod = myProvidedMethodInfosManager.Register(providedNativeModel.GetGetMethod(), providedModelOwner);
      return RdTask<RdProvidedMethodInfo>.Successful(getMethod);
    }
  }
}
