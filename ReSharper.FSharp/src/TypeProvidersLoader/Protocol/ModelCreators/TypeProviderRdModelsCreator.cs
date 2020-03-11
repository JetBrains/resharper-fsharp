using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class TypeProviderRdModelsCreator : ProvidedRdModelsCreatorBase<ITypeProvider, RdTypeProvider>
  {
    public TypeProviderRdModelsCreator(IWriteProvidedCache<Tuple<ITypeProvider, int>> cache) : base(cache)
    {
    }

    protected override RdTypeProvider CreateRdModelInternal(ITypeProvider providedNativeModel, int entityId) =>
      new RdTypeProvider(entityId);
  }
}
