using System;
using System.Reflection;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedAssembly : ProvidedAssembly
  {
    private readonly RdProvidedAssembly myAssembly;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;

    private RdProvidedAssemblyProcessModel RdProvidedAssemblyProcessModel =>
      myProcessModel.RdProvidedAssemblyProcessModel;

    private int EntityId => myAssembly.EntityId;

    private ProxyProvidedAssembly(RdProvidedAssembly assembly, RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context) : base(
      Assembly.GetAssembly(typeof(ProxyProvidedAssembly)), context)
    {
      myAssembly = assembly;
      myProcessModel = processModel;

      myAssemblyName = new Lazy<AssemblyName>(() => ConvertFrom(RdProvidedAssemblyProcessModel.GetName.Sync(EntityId)));
      manifestModuleContent = new Lazy<byte[]>(() => RdProvidedAssemblyProcessModel.GetManifestModuleContents.Sync(EntityId));
    }

    public static ProxyProvidedAssembly CreateWithContext(RdProvidedAssembly assembly,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context) => new ProxyProvidedAssembly(assembly, processModel, context);

    public override string FullName => myAssembly.FullName;

    public override AssemblyName GetName() => myAssemblyName.Value;

    public override byte[] GetManifestModuleContents(ITypeProvider provider) => manifestModuleContent.Value;

    private static AssemblyName ConvertFrom(RdAssemblyName rdAssemblyName)
    {
      var assemblyName = new AssemblyName(rdAssemblyName.Name)
      {
        Version = rdAssemblyName.Version == null ? null : Version.Parse(rdAssemblyName.Version),
        Flags = (AssemblyNameFlags) rdAssemblyName.Flags
      };

      var rdPublicKey = rdAssemblyName.PublicKey;
      if (rdPublicKey == null) return assemblyName;

      if (rdPublicKey.IsKey) assemblyName.SetPublicKey(rdPublicKey.Data);
      else assemblyName.SetPublicKeyToken(rdPublicKey.Data);
      return assemblyName;
    }

    private readonly Lazy<AssemblyName> myAssemblyName;
    private readonly Lazy<byte[]> manifestModuleContent;
  }
}
