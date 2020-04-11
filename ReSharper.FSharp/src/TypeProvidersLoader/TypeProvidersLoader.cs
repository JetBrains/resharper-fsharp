using System;
using System.Linq;
using FSharp.Compiler;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Collections;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader
{
  public class TypeProvidersLoader : ITypeProvidersLoader
  {
    public ITypeProvider[] InstantiateTypeProvidersOfAssembly(InstantiateTypeProvidersOfAssemblyParameters parameters)
    {
      var ilScopeRefOfRuntimeAssembly = parameters.IlScopeRefOfRuntimeAssembly.ToILScopeRef();
      var resolutionEnvironment = parameters.RdResolutionEnvironment.ToResolutionEnvironment();
      var systemRuntimeContainsType = FSharpFunc<string, bool>.FromConverter(x => true);
      var systemRuntimeAssemblyVersion = Version.Parse(parameters.SystemRuntimeAssemblyVersion);
      var compilerToolsPath = ListModule.OfSeq(parameters.CompilerToolsPath);

      var typeProviders = ExtensionTyping.Shim.ExtensionTypingProvider.InstantiateTypeProvidersOfAssembly(
        parameters.RunTimeAssemblyFileName, ilScopeRefOfRuntimeAssembly, parameters.DesignTimeAssemblyNameString,
        resolutionEnvironment, parameters.IsInvalidationSupported, parameters.IsInteractive, systemRuntimeContainsType,
        systemRuntimeAssemblyVersion, compilerToolsPath, Range.range.Zero);

      return typeProviders
        .Select(t =>
          t.PUntaint(FSharpFunc<ITypeProvider, ITypeProvider>.FromConverter(x => x), Range.range.Zero))
        .ToArray();
    }
  }
}
