using System;
using System.Collections.Generic;
using System.IO;
using FSharp.Compiler;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using JetBrains.Util;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using Range = FSharp.Compiler.Text.Range;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host
{
  public interface ITypeProvidersLoader : IDisposable
  {
    IEnumerable<ITypeProvider> InstantiateTypeProvidersOfAssembly(
      InstantiateTypeProvidersOfAssemblyParameters parameters);
  }

  public class TypeProvidersLoader : ITypeProvidersLoader
  {
    private readonly ILogger myLogger;
    private readonly Dictionary<string, string> myShadowCopyMapping = new Dictionary<string, string>();

    public TypeProvidersLoader(ILogger logger)
    {
      myLogger = logger;
    }

    private readonly FSharpFunc<TypeProviderError, Unit> myLogError =
      FSharpFunc<TypeProviderError, Unit>.FromConverter(e =>
        throw new TypeProvidersInstantiationException(e.ContextualErrorMessage, e.Number));

    private string GetAssemblyShadowCopy(string designTimeAssembly)
    {
      if (!myShadowCopyMapping.TryGetValue(designTimeAssembly, out var shadowCopy))
      {
        shadowCopy = Path.ChangeExtension(Path.GetTempFileName(), "dll");
        File.Copy(designTimeAssembly, shadowCopy);
        myLogger.Log(LoggingLevel.INFO, $"Shadow copying assembly {designTimeAssembly} to {shadowCopy}");
        myShadowCopyMapping.Add(designTimeAssembly, shadowCopy);
      }

      return shadowCopy;
    }

    public IEnumerable<ITypeProvider> InstantiateTypeProvidersOfAssembly(
      InstantiateTypeProvidersOfAssemblyParameters parameters)
    {
      var resolutionEnvironment = parameters.RdResolutionEnvironment.ToResolutionEnvironment();
      var systemRuntimeContainsType = TcImportsHack.injectFakeTcImports(parameters.FakeTcImports);
      var systemRuntimeAssemblyVersion = Version.Parse(parameters.SystemRuntimeAssemblyVersion);
      var compilerToolsPath = ListModule.OfSeq(parameters.CompilerToolsPath);

      // todo: comment
      var designTimeAssembly = parameters.ShadowCopyDesignTimeAssembly
        ? GetAssemblyShadowCopy(parameters.RunTimeAssemblyFileName)
        : parameters.DesignTimeAssemblyNameString;

      var typeProviders = ExtensionTyping.Shim.ExtensionTypingProvider.InstantiateTypeProvidersOfAssembly(
        parameters.RunTimeAssemblyFileName, designTimeAssembly,
        resolutionEnvironment, parameters.IsInvalidationSupported, parameters.IsInteractive, systemRuntimeContainsType,
        systemRuntimeAssemblyVersion, compilerToolsPath, myLogError, Range.Zero);
      return typeProviders;
    }

    public void Dispose()
    {
      foreach (var shadowCopy in myShadowCopyMapping.Values)
        if (File.Exists(shadowCopy))
          File.Delete(shadowCopy);
    }
  }
}
