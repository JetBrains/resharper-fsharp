using System;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class
    ProvidedPropertyInfoHostFactory : IOutOfProcessHostFactory<RdProvidedPropertyInfoProcessModel>
  {
    private readonly UnitOfWork myUnitOfWork;

    public ProvidedPropertyInfoHostFactory(UnitOfWork unitOfWork)
    {
      myUnitOfWork = unitOfWork;
    }

    public void Initialize(RdProvidedPropertyInfoProcessModel processModel)
    {
      processModel.PropertyType.Set(GetPropertyType);
      processModel.DeclaringType.Set(GetDeclaringType);
      processModel.GetGetMethod.Set(GetGetMethod);
      processModel.GetSetMethod.Set(GetSetMethod);
      processModel.GetIndexParameters.Set(GetIndexParameters);
    }

    private int? GetDeclaringType(int entityId)
    {
      var (property, typeProviderId) = myUnitOfWork.ProvidedPropertyInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(property.DeclaringType, typeProviderId)?.EntityId;
    }

    private int GetPropertyType(int entityId)
    {
      var (property, typeProviderId) = myUnitOfWork.ProvidedPropertyInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(property.PropertyType, typeProviderId).EntityId;
    }

    private RdProvidedParameterInfo[] GetIndexParameters(int entityId)
    {
      var (property, typeProviderId) = myUnitOfWork.ProvidedPropertyInfosCache.Get(entityId);
      return property
        .GetIndexParameters()
        .CreateRdModels(myUnitOfWork.ProvidedParameterInfoRdModelsCreator, typeProviderId);
    }

    private RdProvidedMethodInfo GetSetMethod(int entityId)
    {
      var (property, typeProviderId) = myUnitOfWork.ProvidedPropertyInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedMethodInfoRdModelsCreator.CreateRdModel(property.GetSetMethod(), typeProviderId);
    }

    private RdProvidedMethodInfo GetGetMethod(int entityId)
    {
      var (property, typeProviderId) = myUnitOfWork.ProvidedPropertyInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedMethodInfoRdModelsCreator.CreateRdModel(property.GetGetMethod(), typeProviderId);
    }
  }
}
