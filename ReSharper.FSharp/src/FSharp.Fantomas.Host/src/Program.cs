using System;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host
{
  public static class Program
  {
    public static void Main(string[] args)
    {
      AppDomain.CurrentDomain.AssemblyResolve += FantomasAssemblyResolver.Resolve;
      MainInternal(args);
    }

    private static void MainInternal(string[] args)
    {
      var endPoint = new FantomasEndPoint();

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
