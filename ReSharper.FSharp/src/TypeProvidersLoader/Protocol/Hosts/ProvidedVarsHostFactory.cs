using System;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedVarsHostFactory : IOutOfProcessHostFactory<RdProvidedVarProcessModel>
  {
    private readonly UnitOfWork myUnitOfWork;

    public ProvidedVarsHostFactory(UnitOfWork unitOfWork)
    {
      myUnitOfWork = unitOfWork;
    }

    public void Initialize(RdProvidedVarProcessModel model)
    {
      model.Type.Set(GetType);
    }

    private int GetType(int entityId)
    {
      var (providedExpr, typeProviderId) = myUnitOfWork.ProvidedVarsCache.Get(entityId);
      return myUnitOfWork.ProvidedTypeRdModelsCreator.CreateRdModel(providedExpr.Type, typeProviderId).EntityId;
    }
  }
}
