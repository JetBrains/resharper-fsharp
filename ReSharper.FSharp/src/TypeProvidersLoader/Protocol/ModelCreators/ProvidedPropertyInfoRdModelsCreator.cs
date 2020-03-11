using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class
    ProvidedPropertyInfoRdModelsCreator : ProvidedRdModelsCreatorBase<ProvidedPropertyInfo, RdProvidedPropertyInfo>
  {
    public ProvidedPropertyInfoRdModelsCreator(IWriteProvidedCache<Tuple<ProvidedPropertyInfo, int>> cache) :
      base(cache)
    {
    }

    protected override RdProvidedPropertyInfo CreateRdModelInternal(ProvidedPropertyInfo providedNativeModel,
      int entityId) => new RdProvidedPropertyInfo(providedNativeModel.CanRead,
      providedNativeModel.CanWrite, providedNativeModel.Name, entityId);
  }
}
