using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class ProvidedEventInfoRdModelsCreator : ProvidedRdModelsCreatorBase<ProvidedEventInfo, RdProvidedEventInfo>
  {
    public ProvidedEventInfoRdModelsCreator(IWriteProvidedCache<Tuple<ProvidedEventInfo, int>> cache) : base(cache)
    {
    }

    protected override RdProvidedEventInfo CreateRdModelInternal(ProvidedEventInfo providedModel, int entityId) =>
      new RdProvidedEventInfo(providedModel.Name, entityId);
  }
}
