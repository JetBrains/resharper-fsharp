using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  //TODO: DELETE
  public class ProvidedAssemblyRdModelCreator : IProvidedRdModelsCreator<ProvidedAssembly, RdProvidedAssembly>
  {
    private readonly IProvidedCache<ProvidedAssembly> myCache;

    public ProvidedAssemblyRdModelCreator(IProvidedCache<ProvidedAssembly> cache)
    {
      myCache = cache;
    }

    [ContractAnnotation("providedModel:null => null")]
    public RdProvidedAssembly CreateRdModel(ProvidedAssembly providedModel, int typeProviderId)
    {
      if (providedModel == null) return null;

      if (!myCache.Contains(typeProviderId)) myCache.Add(typeProviderId, providedModel);
      return new RdProvidedAssembly(providedModel.FullName, providedModel.Handle.Location, typeProviderId);
    }
  }
}
