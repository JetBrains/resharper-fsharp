using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Type = System.Type;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader
{
  public class TypeProvidersLoader : ITypeProvidersLoader
  {
    private readonly Type[] myTpConstructorInfoType = {typeof(TypeProviderConfig)};

    public ITypeProvider[] InstantiateTypeProvidersOfAssembly(
      InstantiateTypeProvidersOfAssemblyParameters parameters)
    {
      //сейчас этот код вызывает FileSystemShim, пока что с дефолтной реализацией 
      var typeProvidersTypes = TypeProviderInstantiateHelpers.GetTypeProviderImplementationTypes(
        parameters.RunTimeAssemblyFileName,
        parameters.DesignTimeAssemblyNameString);
      
      var ids = new List<ITypeProvider>(typeProvidersTypes.Length);

      foreach (var typeProvidersType in typeProvidersTypes)
      {
        try
        {
          var result = CreateTypeProvider(typeProvidersType, parameters);
          ids.Add(result);
        }
        catch (Exception e)
        {
          Console.WriteLine(e);
          throw;
        }
      }

      return ids.ToArray();
    }

    private ITypeProvider CreateTypeProvider(
      Type typeProviderImplementationType,
      InstantiateTypeProvidersOfAssemblyParameters parameters)
    {
      //allocation 
      if (typeProviderImplementationType.GetConstructor(myTpConstructorInfoType) != null)
      {
        var func = FSharpFunc<string, bool>
          .FromConverter(_ => parameters.SystemRuntimeContainsType.SystemRuntimeContainsTypeRef != null);
        var tpConfig = new TypeProviderConfig(func) //(systemRuntimeContainsType)
        {
          ResolutionFolder = parameters.RdResolutionEnvironment.ResolutionFolder,
          RuntimeAssembly = parameters.RunTimeAssemblyFileName,
          ReferencedAssemblies = parameters.RdResolutionEnvironment.ReferencedAssemblies,
          TemporaryFolder = parameters.RdResolutionEnvironment.TemporaryFolder,
          IsInvalidationSupported = parameters.IsInvalidationSupported,
          IsHostedExecution = parameters.IsInteractive,
          SystemRuntimeAssemblyVersion = parameters.SystemRuntimeAssemblyRdVersion.ToSystemVersion(),
        };
        try
        {
          //Hack.getFakeTcImportsTest(func); 
          var typeProvider = Activator.CreateInstance(typeProviderImplementationType, tpConfig) as ITypeProvider;
          return typeProvider;
        }
        catch
        {
        }
      }

      else if (typeProviderImplementationType.GetConstructor(Array.Empty<Type>()) != null)
      {
        var typeProvider = Activator.CreateInstance(typeProviderImplementationType) as ITypeProvider;
        return typeProvider;
      }

      throw new Exception();
      //throw new TypeProviderError(FCSTypeProviderErrors.etProviderDoesNotHaveValidConstructor,
      //  typeProviderImplementationType.FullName, parameters.Range.ToFSharpRange());
    }
  }
}
