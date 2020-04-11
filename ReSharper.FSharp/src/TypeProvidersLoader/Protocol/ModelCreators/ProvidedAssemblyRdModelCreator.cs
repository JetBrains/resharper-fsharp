using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class ProvidedAssemblyRdModelCreator : ProvidedRdModelsCreatorBase<ProvidedAssembly, RdProvidedAssembly>
  {
    public ProvidedAssemblyRdModelCreator(IWriteProvidedCache<Tuple<ProvidedAssembly, int>> cache) : base(cache)
    {
    }

    protected override RdProvidedAssembly CreateRdModelInternal(ProvidedAssembly providedModel, int entityId) =>
      new RdProvidedAssembly(providedModel.FullName, entityId);
  }
}
