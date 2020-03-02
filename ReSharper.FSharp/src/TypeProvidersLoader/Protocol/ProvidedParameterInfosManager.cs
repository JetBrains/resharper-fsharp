using System;
using System.Collections.Generic;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class
    ProvidedParametersManager : OutOfProcessProtocolManagerBase<ProvidedParameterInfo, RdProvidedParameterInfo>
  {
    private readonly IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> myProvidedTypesManager;

    public ProvidedParametersManager(IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> providedTypesManager) :
      base(new ProvidedParameterInfoEqualityComparer())
    {
      myProvidedTypesManager = providedTypesManager;
    }

    protected override RdProvidedParameterInfo CreateProcessModel(
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
      var parameterType = myProvidedTypesManager.Register(providedNativeModel.ParameterType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(parameterType);
    }
  }

  internal class ProvidedParameterInfoEqualityComparer : IEqualityComparer<ProvidedParameterInfo>
  {
    public bool Equals(ProvidedParameterInfo x, ProvidedParameterInfo y)
    {
      return ReferenceEquals(x, y);
    }

    public int GetHashCode(ProvidedParameterInfo obj)
    {
      return obj.Name.GetHashCode();
    }
  }
}
