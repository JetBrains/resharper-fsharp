using System;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedAssemblyHostFactory : IOutOfProcessHostFactory<RdProvidedAssemblyProcessModel>
  {
    private readonly UnitOfWork myUnitOfWork;

    public ProvidedAssemblyHostFactory(UnitOfWork unitOfWork)
    {
      myUnitOfWork = unitOfWork;
    }

    public void Initialize(RdProvidedAssemblyProcessModel model)
    {
      model.GetManifestModuleContents.Set(GetManifestModuleContents);
      model.GetName.Set(GetName);
    }

    private RdAssemblyName GetName(int entityId)
    {
      var (providedAssembly, _) = myUnitOfWork.ProvidedAssembliesCache.Get(entityId);
      var assemblyName = providedAssembly.Handle.GetName();

      var publicKey = assemblyName.GetPublicKey();
      RdPublicKey rdPublicKey;

      if (publicKey != null) rdPublicKey = new RdPublicKey(true, publicKey);
      else
      {
        var publicKeyToken = assemblyName.GetPublicKeyToken();
        rdPublicKey = publicKeyToken != null ? new RdPublicKey(true, publicKeyToken) : null;
      }

      return new RdAssemblyName(assemblyName.Name, rdPublicKey, assemblyName.Version?.ToString(),
        (int) assemblyName.Flags);
    }

    private byte[] GetManifestModuleContents(int entityId)
    {
      var (providedAssembly, typeProviderId) = myUnitOfWork.ProvidedAssembliesCache.Get(entityId);
      var typeProvider = myUnitOfWork.TypeProvidersCache.Get(typeProviderId);
      //TODO: encapsulate handle at provided assembly
      return typeProvider.GetGeneratedAssemblyContents(providedAssembly.Handle);
    }
  }
}
