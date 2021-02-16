using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader
{
  internal static class Program
  {
    public static void Main(string[] args)
    {
      AppDomain.CurrentDomain.AssemblyResolve += RiderPluginAssemblyResolver.Resolve;
      MainInternal(args);
    }

    private static void MainInternal(string[] args)
    {
      var endPoint = new TypeProvidersEndPoint();

      var portValue = args[0];
      var logPath = string.Empty;
      if (args.Length > 1)
      {
        logPath = args[1];
      }

      endPoint.Start(portValue, logPath);
    }
  }
}
