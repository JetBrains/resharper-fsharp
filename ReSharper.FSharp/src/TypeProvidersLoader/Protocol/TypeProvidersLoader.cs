using System;
using System.Collections.Generic;
using FSharp.Compiler;
using JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Exceptions;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using Range = FSharp.Compiler.Text.Range;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public interface ITypeProvidersLoader
  {
    IEnumerable<ITypeProvider> InstantiateTypeProvidersOfAssembly(
      InstantiateTypeProvidersOfAssemblyParameters parameters);
  }

  public class TypeProvidersLoader : ITypeProvidersLoader
  {
    private readonly FSharpFunc<TypeProviderError, Unit> myLogError =
      FSharpFunc<TypeProviderError, Unit>.FromConverter(e =>
        throw new TypeProvidersInstantiationException(e.ContextualErrorMessage, e.Number));

    public IEnumerable<ITypeProvider> InstantiateTypeProvidersOfAssembly(
      InstantiateTypeProvidersOfAssemblyParameters parameters)
    {
      var resolutionEnvironment = parameters.RdResolutionEnvironment.ToResolutionEnvironment();
      var systemRuntimeContainsType = TcImportsHack.injectFakeTcImports(parameters.FakeTcImports);
      var systemRuntimeAssemblyVersion = Version.Parse(parameters.SystemRuntimeAssemblyVersion);
      var compilerToolsPath = ListModule.OfSeq(parameters.CompilerToolsPath);

      var typeProviders = ExtensionTyping.Shim.ExtensionTypingProvider.InstantiateTypeProvidersOfAssembly(
        parameters.RunTimeAssemblyFileName, parameters.DesignTimeAssemblyNameString,
        resolutionEnvironment, parameters.IsInvalidationSupported, parameters.IsInteractive, systemRuntimeContainsType,
        systemRuntimeAssemblyVersion, compilerToolsPath, myLogError, Range.Zero);
      return typeProviders;
    }
  }
}
