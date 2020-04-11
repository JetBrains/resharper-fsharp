using System;
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
    private readonly IReadProvidedCache<Tuple<ProvidedAssembly, int>> myAssembliesCache;
    private readonly IReadProvidedCache<ITypeProvider> myTypeProvidersCache;

    public ProvidedAssemblyHostFactory(IReadProvidedCache<Tuple<ProvidedAssembly, int>> assembliesCache,
      IReadProvidedCache<ITypeProvider> typeProvidersCache)
    {
      myAssembliesCache = assembliesCache;
      myTypeProvidersCache = typeProvidersCache;
    }

    public void Initialize(RdProvidedAssemblyProcessModel model)
    {
      model.GetManifestModuleContents.Set(GetManifestModuleContents);
      model.GetName.Set(GetName);
    }

    private RdTask<RdAssemblyName> GetName(Lifetime lifetime, int entityId)
    {
      var (providedAssembly, _) = myAssembliesCache.Get(entityId);
      var assemblyName = providedAssembly.Handle.GetName();

      var publicKey = assemblyName.GetPublicKey();
      RdPublicKey rdPublicKey;

      if (publicKey != null) rdPublicKey = new RdPublicKey(true, publicKey);
      else
      {
        var publicKeyToken = assemblyName.GetPublicKeyToken();
        rdPublicKey = publicKeyToken != null ? new RdPublicKey(true, publicKeyToken) : null;
      }

      var rdAssemblyName =
        new RdAssemblyName(assemblyName.Name, rdPublicKey, assemblyName.Version.ToString(), (int) assemblyName.Flags);
      return RdTask<RdAssemblyName>.Successful(rdAssemblyName);
    }

    private RdTask<byte[]> GetManifestModuleContents(Lifetime lifetime, int entityId)
    {
      var (providedAssembly, typeProviderId) = myAssembliesCache.Get(entityId);
      var typeProvider = myTypeProvidersCache.Get(typeProviderId);
      return RdTask<byte[]>.Successful(typeProvider.GetGeneratedAssemblyContents(providedAssembly.Handle));
    }
  }
}
