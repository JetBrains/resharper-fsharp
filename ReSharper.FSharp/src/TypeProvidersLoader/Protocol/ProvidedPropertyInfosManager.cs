using System;
using System.Collections.Generic;
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
      IOutOfProcessProtocolManager<ProvidedType, RdProvidedType> providedTypesManager,
      IOutOfProcessProtocolManager<ProvidedMethodInfo, RdProvidedMethodInfo> providedMethodInfosManager) : base(
      new ProvidedPropertyInfoEqualityComparer())
    {
      myProvidedTypesManager = providedTypesManager;
      myProvidedMethodInfosManager = providedMethodInfosManager;
      myProvidedParameterInfosManager = providedParameterInfosManager;
    }

    protected override RdProvidedPropertyInfo CreateProcessModel(
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
      var declaringType = myProvidedTypesManager.Register(providedNativeModel.DeclaringType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(declaringType);
    }

    private RdTask<RdProvidedType> GetPropertyType(
    in Lifetime lifetime, 
    ProvidedPropertyInfo providedNativeModel, 
    ITypeProvider providedModelOwner)
    {
      var propertyType = myProvidedTypesManager.Register(providedNativeModel.PropertyType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(propertyType);
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

  internal class ProvidedPropertyInfoEqualityComparer : IEqualityComparer<ProvidedPropertyInfo>
  {
    public bool Equals(ProvidedPropertyInfo x, ProvidedPropertyInfo y)
    {
      return x.Name == y.Name;
    }

    public int GetHashCode(ProvidedPropertyInfo obj)
    {
      return obj.Name.GetHashCode();
    }
  }
}
