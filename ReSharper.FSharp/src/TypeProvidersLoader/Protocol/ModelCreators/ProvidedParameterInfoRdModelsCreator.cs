using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
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

    protected override RdProvidedParameterInfo CreateRdModelInternal(ProvidedParameterInfo providedNativeModel,
      int entityId) => new RdProvidedParameterInfo(providedNativeModel.IsIn,
      providedNativeModel.IsOut, providedNativeModel.IsOptional,
      providedNativeModel.HasDefaultValue, providedNativeModel.Name, entityId);
  }
}
