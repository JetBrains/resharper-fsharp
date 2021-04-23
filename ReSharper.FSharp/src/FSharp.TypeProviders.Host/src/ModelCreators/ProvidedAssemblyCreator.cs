using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public class ProvidedAssemblyCreator : UniqueProvidedCreatorWithCacheBase<ProvidedAssembly, RdProvidedAssembly>
  {
    public ProvidedAssemblyCreator(TypeProvidersContext context) : base(context.ProvidedAssembliesCache)
    {
    }

    protected override RdProvidedAssembly CreateRdModelInternal(ProvidedAssembly providedModel, int entityId, int _) =>
      new RdProvidedAssembly(providedModel.FullName, GetName(providedModel), entityId);

    private static RdAssemblyName GetName(ProvidedAssembly providedModel)
    {
      var assemblyName = providedModel.Handle.GetName();

      var publicKey = assemblyName.GetPublicKey();
      RdPublicKey rdPublicKey;

      if (publicKey != null) rdPublicKey = new RdPublicKey(true, publicKey);
      else
      {
        var publicKeyToken = assemblyName.GetPublicKeyToken();
        rdPublicKey = publicKeyToken != null ? new RdPublicKey(true, publicKeyToken) : null;
      }

      return new RdAssemblyName(assemblyName.Name!, rdPublicKey, assemblyName.Version?.ToString(),
        (int) assemblyName.Flags);
    }
  }
}
