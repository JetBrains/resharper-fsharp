using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host
{
  public static class FantomasAssemblyResolver
  {
    private const string RiderAdditionalProbingPathsEnvVar = "RIDER_PLUGIN_ADDITIONAL_PROBING_PATHS";
    private const string FantomasAssembliesPathEnvVar = "FSHARP_FANTOMAS_ASSEMBLIES_PATH";
    private static readonly List<string> OurAdditionalProbingPaths = new();

    static FantomasAssemblyResolver()
    {
      var fantomasPath = Environment.GetEnvironmentVariable(FantomasAssembliesPathEnvVar);
      var riderPaths = Environment.GetEnvironmentVariable(RiderAdditionalProbingPathsEnvVar);

      // Cannot use Assertion.Assert in resolver since it's a part of JetBrains.Diagnostics which requires resolve.
      if (string.IsNullOrWhiteSpace(fantomasPath))
        throw new ArgumentException("Argument IsNullOrWhiteSpace", FantomasAssembliesPathEnvVar);
      if (string.IsNullOrWhiteSpace(riderPaths))
        throw new ArgumentException("Argument IsNullOrWhiteSpace", RiderAdditionalProbingPathsEnvVar);

      OurAdditionalProbingPaths.Add(fantomasPath);
      foreach (var path in riderPaths.Split(';'))
        if (!string.IsNullOrEmpty(path))
          OurAdditionalProbingPaths.Add(path);
    }

    public static Assembly Resolve(object sender, ResolveEventArgs eventArgs)
    {
      var assemblyName = $"{new AssemblyName(eventArgs.Name).Name}.dll";

      foreach (var path in OurAdditionalProbingPaths)
      {
        var assemblyPath = Path.Combine(path, assemblyName);
        if (!File.Exists(assemblyPath)) continue;
        return Assembly.LoadFrom(assemblyPath);
      }

      Console.Error.Write($"\nFailed to resolve assembly by name '{eventArgs.Name}'" +
                          $"\n  Requesting assembly: {eventArgs.RequestingAssembly?.FullName}" +
                          $"\n  Probing paths: {string.Join("\n", OurAdditionalProbingPaths)}");
      return null;
    }
  }
}
