using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host
{
  public static class TypeProvidersAssemblyResolver
  {
    private const string AdditionalProbingPathsEnvVar = "RIDER_PLUGIN_ADDITIONAL_PROBING_PATHS";
    private static readonly List<string> OurAdditionalProbingPaths = new List<string>();

    static TypeProvidersAssemblyResolver()
    {
      var paths = Environment.GetEnvironmentVariable(AdditionalProbingPathsEnvVar);
      if (string.IsNullOrWhiteSpace(paths)) return;

      foreach (var path in paths.Split(';'))
      {
        if (!string.IsNullOrEmpty(path)) OurAdditionalProbingPaths.Add(path);
      }
    }

    public static Assembly Resolve(object sender, ResolveEventArgs eventArgs)
    {
      foreach (var path in OurAdditionalProbingPaths)
      {
        var assemblyPath = Path.Combine(path, $"{new AssemblyName(eventArgs.Name).Name}.dll");
        if (!File.Exists(assemblyPath)) continue;

        var assembly = Assembly.LoadFrom(assemblyPath);
        return assembly;
      }

      Console.Error.Write($"\nFailed to resolve assembly by name '{eventArgs.Name}'" +
                          $"\n  Requesting assembly: {eventArgs.RequestingAssembly?.FullName}" +
                          $"\n  Probing paths: {string.Join("\n", OurAdditionalProbingPaths)}");
      return null;
    }
  }
}
