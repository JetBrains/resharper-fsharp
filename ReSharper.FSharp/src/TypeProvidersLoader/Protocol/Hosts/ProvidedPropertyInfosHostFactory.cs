using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class
    ProvidedPropertyInfoHost : IOutOfProcessHostFactory<RdProvidedPropertyInfoProcessModel>
  {
    private readonly IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo>
      myProvidedParameterInfosHost;

    private readonly IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo>
      myProvidedMethodInfosHost;

    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedTypesHost;

    public RdProvidedPropertyInfoProcessModel CreateProcessModel()
    {
      var processModel = new RdProvidedPropertyInfoProcessModel();

      processModel.PropertyType.Set(GetPropertyType);
      processModel.DeclaringType.Set(GetDeclaringType);
      processModel.GetGetMethod.Set(GetGetMethod);
      processModel.GetSetMethod.Set(GetSetMethod);
      processModel.GetIndexParameters.Set((lifetime, _) =>
        GetIndexParameters(lifetime, providedNativeModel, providedModelOwner));

      return ppModel;
    }
    
    public ProvidedPropertyInfoHost(
      IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo>
        providedParameterInfosHost,
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypesHost,
      IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo> providedMethodInfosHost) : base(
      new ProvidedPropertyInfoEqualityComparer())
    {
      myProvidedTypesHost = providedTypesHost;
      myProvidedMethodInfosHost = providedMethodInfosHost;
      myProvidedParameterInfosHost = providedParameterInfosHost;
    }

    private RdTask<RdProvidedType> GetDeclaringType(
      in Lifetime lifetime, 
      ProvidedPropertyInfo providedNativeModel, 
      ITypeProvider providedModelOwner)
    {
      var declaringType = myProvidedTypesHost.CreateRdModel(providedNativeModel.DeclaringType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(declaringType);
    }

    private RdTask<RdProvidedType> GetPropertyType(
    in Lifetime lifetime, 
    ProvidedPropertyInfo providedNativeModel, 
    ITypeProvider providedModelOwner)
    {
      var propertyType = myProvidedTypesHost.CreateRdModel(providedNativeModel.PropertyType, providedModelOwner);
      return RdTask<RdProvidedType>.Successful(propertyType);
    }

    private RdTask<RdProvidedParameterInfo[]> GetIndexParameters(
      in Lifetime lifetime,
      ProvidedPropertyInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var indexParameters = providedNativeModel
        .GetIndexParameters()
        .Select(t => myProvidedParameterInfosHost.CreateRdModel(t, providedModelOwner))
        .ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(indexParameters);
    }

    private RdTask<RdProvidedMethodInfo> GetSetMethod(
      in Lifetime lifetime,
      ProvidedPropertyInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var setMethod = myProvidedMethodInfosHost.CreateRdModel(providedNativeModel.GetSetMethod(), providedModelOwner);
      return RdTask<RdProvidedMethodInfo>.Successful(setMethod);
    }

    private RdTask<RdProvidedMethodInfo> GetGetMethod(
      in Lifetime lifetime,
      ProvidedPropertyInfo providedNativeModel,
      ITypeProvider providedModelOwner)
    {
      var getMethod = myProvidedMethodInfosHost.CreateRdModel(providedNativeModel.GetGetMethod(), providedModelOwner);
      return RdTask<RdProvidedMethodInfo>.Successful(getMethod);
    }
  }
}
