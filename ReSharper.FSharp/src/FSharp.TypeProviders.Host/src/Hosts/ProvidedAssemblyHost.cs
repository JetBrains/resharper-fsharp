using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Hosts
{
  internal class ProvidedAssemblyHost : IOutOfProcessHost<RdProvidedAssemblyProcessModel>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public ProvidedAssemblyHost(TypeProvidersContext typeProvidersContext) =>
      myTypeProvidersContext = typeProvidersContext;

    public void Initialize(RdProvidedAssemblyProcessModel model)
    {
      model.GetProvidedAssembly.Set(GetProvidedAssembly);
      model.GetManifestModuleContents.Set(GetManifestModuleContents);
    }

    private RdProvidedAssembly GetProvidedAssembly(int entityId)
    {
      var (providedAssembly, typeProviderId) = myTypeProvidersContext.ProvidedAssembliesCache.Get(entityId);
      return myTypeProvidersContext.ProvidedAssemblyRdModelsCreator.CreateRdModel(providedAssembly, typeProviderId);
    }

    private byte[] GetManifestModuleContents(int entityId)
    {
      var (providedAssembly, typeProviderId) = myTypeProvidersContext.ProvidedAssembliesCache.Get(entityId);
      var typeProvider = myTypeProvidersContext.TypeProvidersCache.Get(typeProviderId);
      return typeProvider.GetGeneratedAssemblyContents(providedAssembly.Handle);
    }
  }
}
