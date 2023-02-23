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
    private readonly FSharpFunc<TypeProviderError, Unit> myLogError =
      FSharpFunc<TypeProviderError, Unit>.FromConverter(e =>
        throw new TypeProvidersInstantiationException(e.ContextualErrorMessage, e.Number));

    private static global::FSharp.Compiler.TypeProviders.ResolutionEnvironment
      ToResolutionEnvironment(RdResolutionEnvironment env) =>
      new(env.ResolutionFolder,
        OptionModule.OfObj(env.OutputFile),
        env.ShowResolutionMessages,
        new GetReferencedAssembliesCallback(env.ReferencedAssemblies),
        env.TemporaryFolder);

    public IEnumerable<ITypeProvider> InstantiateTypeProvidersOfAssembly(
      InstantiateTypeProvidersOfAssemblyParameters parameters)
    {
      var resolutionEnvironment = ToResolutionEnvironment(parameters.RdResolutionEnvironment);
      var systemRuntimeContainsType = TcImportsHack.InjectFakeTcImports(parameters.FakeTcImports);
      var systemRuntimeAssemblyVersion = Version.Parse(parameters.SystemRuntimeAssemblyVersion);
      var compilerToolsPath = ListModule.OfSeq(parameters.CompilerToolsPath);

      var typeProviders = ExtensionTyping.Provider.InstantiateTypeProvidersOfAssembly(
        parameters.RunTimeAssemblyFileName, parameters.DesignTimeAssemblyNameString,
        resolutionEnvironment, parameters.IsInvalidationSupported, parameters.IsInteractive, systemRuntimeContainsType,
        systemRuntimeAssemblyVersion, compilerToolsPath, myLogError, Range.Zero);
      return typeProviders;
    }
  }

  internal class GetReferencedAssembliesCallback : FSharpFunc<Unit, string[]>
  {
    private readonly string[] myReferencedAssemblies;

    public GetReferencedAssembliesCallback(string[] referencedAssemblies) =>
      myReferencedAssemblies = referencedAssemblies;

    public override string[] Invoke(Unit func) => myReferencedAssemblies;
  }
}
