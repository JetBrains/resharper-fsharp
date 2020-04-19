using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class ProvidedVarRdModelsCreator : ProvidedRdModelsCreatorBase<ProvidedVar, RdProvidedVar>
  {
    public ProvidedVarRdModelsCreator(IWriteProvidedCache<Tuple<ProvidedVar, int>> cache) : base(cache)
    {
    }

    protected override RdProvidedVar CreateRdModelInternal(ProvidedVar providedModel, int entityId) =>
      new RdProvidedVar(providedModel.IsMutable, providedModel.Name, entityId);
  }
}
