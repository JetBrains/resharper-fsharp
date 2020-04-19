using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class ProvidedExprRdModelsCreator : ProvidedRdModelsCreatorBase<ProvidedExpr, RdProvidedExpr>
  {
    public ProvidedExprRdModelsCreator(IWriteProvidedCache<Tuple<ProvidedExpr, int>> cache) : base(cache)
    {
    }

    protected override RdProvidedExpr CreateRdModelInternal(ProvidedExpr providedModel, int entityId) =>
      new RdProvidedExpr(providedModel.UnderlyingExpressionString, entityId);
  }
}
