using System;
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
  public class
    ProvidedPropertyInfoHost : OutOfProcessProtocolHostBase<ProvidedPropertyInfo, RdProvidedPropertyInfo>
  {
    private readonly IOutOfProcessProtocolHost<ProvidedParameterInfo, RdProvidedParameterInfo>
      myProvidedParameterInfosHost;

    private readonly IOutOfProcessProtocolHost<ProvidedMethodInfo, RdProvidedMethodInfo>
      myProvidedMethodInfosHost;

    private readonly IOutOfProcessProtocolHost<ProvidedType, RdProvidedType> myProvidedTypesHost;

    public ProvidedPropertyInfoHost(
      IOutOfProcessProtocolHost<ProvidedParameterInfo, RdProvidedParameterInfo>
        providedParameterInfosHost,
      IOutOfProcessProtocolHost<ProvidedType, RdProvidedType> providedTypesHost,
      IOutOfProcessProtocolHost<ProvidedMethodInfo, RdProvidedMethodInfo> providedMethodInfosHost) : base(
      new ProvidedPropertyInfoEqualityComparer())
    {
      myProvidedTypesHost = providedTypesHost;
      myProvidedMethodInfosHost = providedMethodInfosHost;
      myProvidedParameterInfosHost = providedParameterInfosHost;
    }

    protected override RdProvidedPropertyInfo CreateRdModel(
      ProvidedPropertyInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var ppModel = new RdProvidedPropertyInfo(
        providedNativeModel.CanRead,
        providedNativeModel.CanWrite,
        providedNativeModel.Name);

      ppModel.PropertyType.Set((lifetime, _) => GetPropertyType(lifetime, providedNativeModel, providedModelOwner));
      ppModel.DeclaringType.Set((lifetime, _) => GetDeclaringType(lifetime, providedNativeModel, providedModelOwner));
      ppModel.GetGetMethod.Set((lifetime, _) => GetGetMethod(lifetime, providedNativeModel, providedModelOwner));
      ppModel.GetSetMethod.Set((lifetime, _) => GetSetMethod(lifetime, providedNativeModel, providedModelOwner));
      ppModel.GetIndexParameters.Set((lifetime, _) =>
        GetIndexParameters(lifetime, providedNativeModel, providedModelOwner));

      return ppModel;
    }

    private RdTask<RdProvidedType> GetDeclaringType(
      in Lifetime lifetime, 
      ProvidedPropertyInfo providedNativeModel, 
      ITypeProvider providedModelOwner)
    {
      var declaringType = myProvidedTypesHost.GetRdModel(providedNativeModel.DeclaringType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(declaringType);
    }

    private RdTask<RdProvidedType> GetPropertyType(
    in Lifetime lifetime, 
    ProvidedPropertyInfo providedNativeModel, 
    ITypeProvider providedModelOwner)
    {
      var propertyType = myProvidedTypesHost.GetRdModel(providedNativeModel.PropertyType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(propertyType);
    }

    private RdTask<RdProvidedParameterInfo[]> GetIndexParameters(
      in Lifetime lifetime,
      ProvidedPropertyInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var indexParameters = providedNativeModel
        .GetIndexParameters()
        .Select(t => myProvidedParameterInfosHost.GetRdModel(t, providedModelOwner))
        .ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(indexParameters);
    }

    private RdTask<RdProvidedMethodInfo> GetSetMethod(
      in Lifetime lifetime,
      ProvidedPropertyInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var setMethod = myProvidedMethodInfosHost.GetRdModel(providedNativeModel.GetSetMethod(), providedModelOwner);
      return RdTask<RdProvidedMethodInfo>.Successful(setMethod);
    }

    private RdTask<RdProvidedMethodInfo> GetGetMethod(
      in Lifetime lifetime,
      ProvidedPropertyInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var getMethod = myProvidedMethodInfosHost.GetRdModel(providedNativeModel.GetGetMethod(), providedModelOwner);
      return RdTask<RdProvidedMethodInfo>.Successful(getMethod);
    }
  }
}
