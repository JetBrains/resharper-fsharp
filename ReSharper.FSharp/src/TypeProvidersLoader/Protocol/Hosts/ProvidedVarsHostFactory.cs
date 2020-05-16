using System;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedVarsHostFactory : IOutOfProcessHostFactory<RdProvidedVarProcessModel>
  {
    private readonly IReadProvidedCache<Tuple<ProvidedVar, int>> myProvidedVarsCache;
    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedTypeRdModelsCreator;

    public ProvidedVarsHostFactory(IReadProvidedCache<Tuple<ProvidedVar, int>> providedVarsCache,
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypeRdModelsCreator)
    {
      myProvidedVarsCache = providedVarsCache;
      myProvidedTypeRdModelsCreator = providedTypeRdModelsCreator;
    }

    public void Initialize(RdProvidedVarProcessModel model)
    {
      model.Type.Set(GetType);
    }

    private int GetType(int entityId)
    {
      var (providedExpr, typeProviderId) = myProvidedVarsCache.Get(entityId);
      return myProvidedTypeRdModelsCreator.CreateRdModel(providedExpr.Type, typeProviderId).EntityId;
    }
  }
}
