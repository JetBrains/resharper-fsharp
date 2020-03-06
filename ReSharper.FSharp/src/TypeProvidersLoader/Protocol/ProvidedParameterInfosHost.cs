using System;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class
    ProvidedParametersHost : OutOfProcessProtocolHostBase<ProvidedParameterInfo, RdProvidedParameterInfo>
  {
    private readonly IOutOfProcessProtocolHost<ProvidedType, RdProvidedType> myProvidedTypesHost;

    public ProvidedParametersHost(IOutOfProcessProtocolHost<ProvidedType, RdProvidedType> providedTypesHost) :
      base(new ProvidedParameterInfoEqualityComparer())
    {
      myProvidedTypesHost = providedTypesHost;
    }

    protected override RdProvidedParameterInfo CreateRdModel(
      ProvidedParameterInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var parameterModel = new RdProvidedParameterInfo(providedNativeModel.Name,
        providedNativeModel.IsIn,
        providedNativeModel.IsOut,
        providedNativeModel.IsOptional,
        providedNativeModel.HasDefaultValue);

      parameterModel.ParameterType.Set((lifetime, _) =>
        GetParameterType(lifetime, providedNativeModel, providedModelOwner));

      return parameterModel;
    }

    private RdTask<RdProvidedType> GetParameterType(
      in Lifetime lifetime,
      ProvidedParameterInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var parameterType = myProvidedTypesHost.GetRdModel(providedNativeModel.ParameterType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(parameterType);
    }
  }
}
