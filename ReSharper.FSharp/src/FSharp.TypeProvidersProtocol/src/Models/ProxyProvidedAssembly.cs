using System.Reflection;
using FSharp.Compiler;
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

    public ProxyProvidedAssembly(RdProvidedAssembly assembly, RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context) : base(
      Assembly.GetAssembly(typeof(string)), context)
    {
      myAssembly = assembly;
      myProcessModel = processModel;
    }

    public override string FullName => myAssembly.FullName;

    public override AssemblyName GetName()
    {
      var assemblyPath = RdProvidedAssemblyProcessModel.GetAssemblyPath.Sync(EntityId);
      return AssemblyName.GetAssemblyName(assemblyPath);
    }

    public override byte[] GetManifestModuleContents(ITypeProvider provider) =>
      RdProvidedAssemblyProcessModel.GetManifestModuleContents.Sync(EntityId);
  }
}
