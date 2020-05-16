using System;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class
    ProvidedPropertyInfoHostFactory : IOutOfProcessHostFactory<RdProvidedPropertyInfoProcessModel>
  {
    private readonly IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo>
      myProvidedParameterInfosHost;

    private readonly IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo>
      myProvidedMethodInfosHost;

    private readonly IReadProvidedCache<Tuple<ProvidedPropertyInfo, int>> myProvidedPropertiesCache;

    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedTypesHost;

    public void Initialize(RdProvidedPropertyInfoProcessModel processModel)
    {
      processModel.PropertyType.Set(GetPropertyType);
      processModel.DeclaringType.Set(GetDeclaringType);
      processModel.GetGetMethod.Set(GetGetMethod);
      processModel.GetSetMethod.Set(GetSetMethod);
      processModel.GetIndexParameters.Set(GetIndexParameters);
    }

    public ProvidedPropertyInfoHostFactory(
      IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo> providedParameterInfosHost,
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypesHost,
      IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo> providedMethodInfosHost,
      IReadProvidedCache<Tuple<ProvidedPropertyInfo, int>> providedPropertiesCache)
    {
      myProvidedTypesHost = providedTypesHost;
      myProvidedMethodInfosHost = providedMethodInfosHost;
      myProvidedPropertiesCache = providedPropertiesCache;
      myProvidedParameterInfosHost = providedParameterInfosHost;
    }

    private int? GetDeclaringType(int entityId)
    {
      var (property, typeProviderId) = myProvidedPropertiesCache.Get(entityId);
      return myProvidedTypesHost.CreateRdModel(property.DeclaringType, typeProviderId)?.EntityId;
    }

    private int GetPropertyType(int entityId)
    {
      var (property, typeProviderId) = myProvidedPropertiesCache.Get(entityId);
      return myProvidedTypesHost.CreateRdModel(property.PropertyType, typeProviderId).EntityId;
    }

    private RdProvidedParameterInfo[] GetIndexParameters(int entityId)
    {
      var (property, typeProviderId) = myProvidedPropertiesCache.Get(entityId);
      return property
        .GetIndexParameters()
        .CreateRdModels(myProvidedParameterInfosHost, typeProviderId);
    }

    private RdProvidedMethodInfo GetSetMethod(int entityId)
    {
      var (property, typeProviderId) = myProvidedPropertiesCache.Get(entityId);
      return myProvidedMethodInfosHost.CreateRdModel(property.GetSetMethod(), typeProviderId);
    }

    private RdProvidedMethodInfo GetGetMethod(int entityId)
    {
      var (property, typeProviderId) = myProvidedPropertiesCache.Get(entityId);
      return myProvidedMethodInfosHost.CreateRdModel(property.GetGetMethod(), typeProviderId);
    }
  }
}
