using System;
using System.Reflection;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using JetBrains.Util.Concurrency;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public class ProxyProvidedAssembly : ProvidedAssembly
  {
    private readonly RdProvidedAssembly myAssembly;
    private readonly TypeProvidersConnection myTypeProvidersConnection;

    private RdProvidedAssemblyProcessModel RdProvidedAssemblyProcessModel =>
      myTypeProvidersConnection.ProtocolModel.RdProvidedAssemblyProcessModel;

    private int EntityId => myAssembly.EntityId;

    private ProxyProvidedAssembly(RdProvidedAssembly assembly, TypeProvidersConnection typeProvidersConnection)
      : base(null)
    {
      myAssembly = assembly;
      myTypeProvidersConnection = typeProvidersConnection;

      myManifestModuleContent = new InterruptibleLazy<byte[]>(() =>
        myTypeProvidersConnection.ExecuteWithCatch(() =>
          RdProvidedAssemblyProcessModel.GetManifestModuleContents.Sync(EntityId, RpcTimeouts.Maximal)));
    }

    public static ProxyProvidedAssembly Create(RdProvidedAssembly assembly, TypeProvidersConnection connection) =>
      new ProxyProvidedAssembly(assembly, connection);

    public override string FullName => myAssembly.FullName;

    public override AssemblyName GetName() => myAssemblyName ??= ConvertFrom(myAssembly.AssemblyName);

    public override byte[] GetManifestModuleContents(ITypeProvider provider) => myManifestModuleContent.Value;

    private static AssemblyName ConvertFrom(RdAssemblyName rdAssemblyName)
    {
      var (name, rdPublicKey, version, flags) = rdAssemblyName;
      var assemblyName = new AssemblyName(name)
      {
        // Version can be null for generated runtime assemblies
        Version = version == null ? null : Version.Parse(version),
        Flags = (AssemblyNameFlags) flags
      };

      if (rdPublicKey == null) return assemblyName;

      if (rdPublicKey.IsKey) assemblyName.SetPublicKey(rdPublicKey.Data);
      else assemblyName.SetPublicKeyToken(rdPublicKey.Data);
      return assemblyName;
    }

    private readonly InterruptibleLazy<byte[]> myManifestModuleContent;
    private AssemblyName myAssemblyName;
  }
}
