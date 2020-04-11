using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class
    ProvidedParameterInfoRdModelsCreator : ProvidedRdModelsCreatorBase<ProvidedParameterInfo, RdProvidedParameterInfo>
  {
    public ProvidedParameterInfoRdModelsCreator(IWriteProvidedCache<Tuple<ProvidedParameterInfo, int>> cache) :
      base(cache)
    {
    }

    protected override RdProvidedParameterInfo CreateRdModelInternal(ProvidedParameterInfo providedModel,
      int entityId) => new RdProvidedParameterInfo(providedModel.IsIn,
      providedModel.IsOut, providedModel.IsOptional,
      providedModel.RawDefaultValue.BoxToClientStaticArg(), providedModel.HasDefaultValue,
      providedModel.Name, entityId);
  }
}
