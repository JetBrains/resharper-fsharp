using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class ProvidedNamespaceRdModelCreator : ProvidedRdModelsCreatorBase<IProvidedNamespace, RdProvidedNamespace>
  {
    public ProvidedNamespaceRdModelCreator(IWriteProvidedCache<Tuple<IProvidedNamespace, int>> cache) : base(cache)
    {
    }

    protected override RdProvidedNamespace
      CreateRdModelInternal(IProvidedNamespace providedModel, int entityId) =>
      new RdProvidedNamespace(providedModel.NamespaceName, entityId);
  }
}
