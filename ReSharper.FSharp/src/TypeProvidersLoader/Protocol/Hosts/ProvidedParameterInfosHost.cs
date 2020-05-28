using System;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedParameterInfosHostFactory : IOutOfProcessHostFactory<RdProvidedParameterInfoProcessModel>
  {
    private readonly UnitOfWork myUnitOfWork;

    public ProvidedParameterInfosHostFactory(UnitOfWork unitOfWork)
    {
      myUnitOfWork = unitOfWork;
    }

    public void Initialize(RdProvidedParameterInfoProcessModel processModel)
    {
      processModel.ParameterType.Set(GetParameterType);
    }

    private int GetParameterType(int entityId)
    {
      var (providedParameter, typeProviderId) = myUnitOfWork.ProvidedParameterInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedParameter.ParameterType, typeProviderId)
        .EntityId;
    }
  }
}
