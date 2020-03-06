using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class ProvidedMethodInfosHost : OutOfProcessProtocolHostBase<ProvidedMethodInfo, RdProvidedMethodInfo>
  {
    private readonly IOutOfProcessProtocolHost<ProvidedType, RdProvidedType> myProvidedTypesHost;

    private readonly IOutOfProcessProtocolHost<ProvidedParameterInfo, RdProvidedParameterInfo>
      myProvidedParameterInfosHost;

    public ProvidedMethodInfosHost(IOutOfProcessProtocolHost<ProvidedType, RdProvidedType> providedTypesHost,
      IOutOfProcessProtocolHost<ProvidedParameterInfo, RdProvidedParameterInfo> providedParameterInfosHost) :
      base(new ProvidedMethodInfoEqualityComparer())
    {
      myProvidedTypesHost = providedTypesHost;
      myProvidedParameterInfosHost = providedParameterInfosHost;
    }

    protected override RdProvidedMethodInfo CreateRdModel(
      ProvidedMethodInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var methodInfoModel = new RdProvidedMethodInfo(
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
        providedNativeModel.Name);

      methodInfoModel.ReturnType.Set((lifetime, _) =>
        GetReturnType(lifetime, providedNativeModel, providedModelOwner));
      methodInfoModel.DeclaringType.Set((lifetime, _) =>
        GetDeclaringType(lifetime, providedNativeModel, providedModelOwner));
      methodInfoModel.GetParameters.Set((lifetime, _) =>
        GetParameters(lifetime, providedNativeModel, providedModelOwner));
      methodInfoModel.GetGenericArguments.Set((lifetime, _) =>
        GetGenericArguments(lifetime, providedNativeModel, providedModelOwner));

      return methodInfoModel;
    }

    private RdTask<RdProvidedType> GetDeclaringType(
      in Lifetime lifetime,
      ProvidedMethodInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var declaringType = myProvidedTypesHost.GetRdModel(providedNativeModel.DeclaringType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(declaringType);
    }

    private RdTask<RdProvidedType> GetReturnType(
      in Lifetime lifetime,
      ProvidedMethodInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var declaringType = myProvidedTypesHost.GetRdModel(providedNativeModel.DeclaringType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(declaringType);
    }

    private RdTask<RdProvidedType[]> GetGenericArguments(
      in Lifetime lifetime,
      ProvidedMethodInfo providedMethod,
      ITypeProvider providedModelOwner)
    {
      var genericArgs = providedMethod
        .GetGenericArguments()
        .Select(t => myProvidedTypesHost.GetRdModel(t, providedModelOwner)).ToArray();
      return RdTask<RdProvidedType[]>.Successful(genericArgs);
    }

    private RdTask<RdProvidedParameterInfo[]> GetParameters(
      in Lifetime lifetime,
      ProvidedMethodInfo providedMethod,
      ITypeProvider providedModelOwner)
    {
      var parameters = providedMethod
        .GetParameters()
        .Select(t => myProvidedParameterInfosHost.GetRdModel(t, providedModelOwner)).ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(parameters);
    }
  }
}
