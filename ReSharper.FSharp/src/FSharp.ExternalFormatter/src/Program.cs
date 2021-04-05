using System;

namespace FSharp.ExternalFormatter
{
  public static class Program
  {
    public static void Main(string[] args)
    {
      AppDomain.CurrentDomain.AssemblyResolve += ExternalFormatterAssemblyResolver.Resolve;
      MainInternal(args);
    }

    private static void MainInternal(string[] args)
    {
      var endPoint = new ExternalFormatterEndPoint();

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
