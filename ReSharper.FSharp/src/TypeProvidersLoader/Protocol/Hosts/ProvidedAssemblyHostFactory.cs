using FSharp.Compiler;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedAssemblyHostFactory : IOutOfProcessHostFactory<RdProvidedAssemblyProcessModel>
  {
    private readonly IReadProvidedCache<ProvidedAssembly> myAssembliesCache;
    private readonly IReadProvidedCache<ITypeProvider> myTypeProvidersCache;

    public ProvidedAssemblyHostFactory(IReadProvidedCache<ProvidedAssembly> assembliesCache,
      IReadProvidedCache<ITypeProvider> typeProvidersCache)
    {
      myAssembliesCache = assembliesCache;
      myTypeProvidersCache = typeProvidersCache;
    }

    public void Initialize(RdProvidedAssemblyProcessModel model)
    {
      model.GetManifestModuleContents.Set(GetManifestModuleContents);
    }

    private RdTask<byte[]> GetManifestModuleContents(Lifetime lifetime, int entityId)
    {
      var providedAssembly = myAssembliesCache.Get(entityId);
      var typeProvider = myTypeProvidersCache.Get(entityId);
      return RdTask<byte[]>.Successful(typeProvider.GetGeneratedAssemblyContents(providedAssembly.Handle));
    }
  }
}
