using System;
using System.Linq;
using JetBrains.Lifetimes;
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

    private RdTask<int?> GetDeclaringType(Lifetime lifetime, int entityId)
    {
      var (property, typeProviderId) = myProvidedPropertiesCache.Get(entityId);
      var declaringType = myProvidedTypesHost.CreateRdModel(property.DeclaringType, typeProviderId)?.EntityId;
      return RdTask<int?>.Successful(declaringType);
    }

    private RdTask<int> GetPropertyType(Lifetime lifetime, int entityId)
    {
      var (property, typeProviderId) = myProvidedPropertiesCache.Get(entityId);
      var propertyType = myProvidedTypesHost.CreateRdModel(property.PropertyType, typeProviderId).EntityId;
      return RdTask<int>.Successful(propertyType);
    }

    private RdTask<RdProvidedParameterInfo[]> GetIndexParameters(Lifetime lifetime, int entityId)
    {
      var (property, typeProviderId) = myProvidedPropertiesCache.Get(entityId);
      var indexParameters = property
        .GetIndexParameters()
        .Select(t => myProvidedParameterInfosHost.CreateRdModel(t, typeProviderId))
        .ToArray();
      return RdTask<RdProvidedParameterInfo[]>.Successful(indexParameters);
    }

    private RdTask<RdProvidedMethodInfo> GetSetMethod(Lifetime lifetime, int entityId)
    {
      var (property, typeProviderId) = myProvidedPropertiesCache.Get(entityId);
      var setMethod = myProvidedMethodInfosHost.CreateRdModel(property.GetSetMethod(), typeProviderId);
      return RdTask<RdProvidedMethodInfo>.Successful(setMethod);
    }

    private RdTask<RdProvidedMethodInfo> GetGetMethod(Lifetime lifetime, int entityId)
    {
      var (property, typeProviderId) = myProvidedPropertiesCache.Get(entityId);
      var getMethod = myProvidedMethodInfosHost.CreateRdModel(property.GetGetMethod(), typeProviderId);
      return RdTask<RdProvidedMethodInfo>.Successful(getMethod);
    }
  }
}
