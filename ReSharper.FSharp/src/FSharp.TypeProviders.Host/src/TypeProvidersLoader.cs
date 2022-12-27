using System;
using System.Collections.Generic;
using FSharp.Compiler;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using Range = FSharp.Compiler.Text.Range;
using static FSharp.Compiler.TypeProviders.Shim;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host
{
  public interface ITypeProvidersLoader
  {
    IEnumerable<ITypeProvider> InstantiateTypeProvidersOfAssembly(
      InstantiateTypeProvidersOfAssemblyParameters parameters);
  }

  public class TypeProvidersLoader : ITypeProvidersLoader
  {
    private readonly RdTypeProviderProcessModel myModel;

    private readonly FSharpFunc<TypeProviderError, Unit> myLogError =
      FSharpFunc<TypeProviderError, Unit>.FromConverter(e =>
        throw new TypeProvidersInstantiationException(e.ContextualErrorMessage, e.Number));

    public TypeProvidersLoader(RdTypeProviderProcessModel model) => myModel = model;

    private static global::FSharp.Compiler.TypeProviders.ResolutionEnvironment
      ToResolutionEnvironment(RdResolutionEnvironment env, GetReferencedAssembliesCallback callback) =>
      new(env.ResolutionFolder,
        OptionModule.OfObj(env.OutputFile),
        env.ShowResolutionMessages,
        callback,
        env.TemporaryFolder);

    public IEnumerable<ITypeProvider> InstantiateTypeProvidersOfAssembly(
      InstantiateTypeProvidersOfAssemblyParameters parameters)
    {
      var getReferencedAssemblies = new GetReferencedAssembliesCallback(myModel, parameters.EnvironmentPath);
      var resolutionEnvironment = ToResolutionEnvironment(parameters.RdResolutionEnvironment, getReferencedAssemblies);
      var systemRuntimeContainsType = TcImportsHack.InjectFakeTcImports(parameters.FakeTcImports);
      var systemRuntimeAssemblyVersion = Version.Parse(parameters.SystemRuntimeAssemblyVersion);
      var compilerToolsPath = ListModule.OfSeq(parameters.CompilerToolsPath);

      var typeProviders = ExtensionTyping.Provider.InstantiateTypeProvidersOfAssembly(
        parameters.RunTimeAssemblyFileName, parameters.DesignTimeAssemblyNameString,
        resolutionEnvironment, parameters.IsInvalidationSupported, parameters.IsInteractive, systemRuntimeContainsType,
        systemRuntimeAssemblyVersion, compilerToolsPath, myLogError, Range.Zero);
      return typeProviders;
    }

    private class GetReferencedAssembliesCallback : FSharpFunc<Unit, string[]>
    {
      private readonly RdTypeProviderProcessModel myModel;
      private readonly string myEnvKey;

      public GetReferencedAssembliesCallback(RdTypeProviderProcessModel model, string envKey)
      {
        myModel = model;
        myEnvKey = envKey;
      }

      public override string[] Invoke(Unit func) => myModel.GetReferencedAssemblies.Sync(myEnvKey);
    }
  }
}
