using System;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedFieldInfoHostFactory : IOutOfProcessHostFactory<RdProvidedFieldInfoProcessModel>
  {
    private readonly UnitOfWork myUnitOfWork;

    public ProvidedFieldInfoHostFactory(UnitOfWork unitOfWork)
    {
      myUnitOfWork = unitOfWork;
    }

    public void Initialize(RdProvidedFieldInfoProcessModel model)
    {
      model.FieldType.Set(GetFieldType);
      model.DeclaringType.Set(GetDeclaringType);
    }

    private int GetDeclaringType(int entityId)
    {
      var (providedType, typeProviderId) = myUnitOfWork.ProvidedFieldInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedType.DeclaringType, typeProviderId)
        .EntityId;
    }

    private int GetFieldType(int entityId)
    {
      var (providedType, typeProviderId) = myUnitOfWork.ProvidedFieldInfosCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedType.FieldType, typeProviderId).EntityId;
    }
  }
}
