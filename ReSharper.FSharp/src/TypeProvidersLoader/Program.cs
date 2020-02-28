using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader
{
  internal class Program
  {
    public static void Main(string[] args)
    {
      var endPoint = new OutOfProcessTypeProvidersLoaderEndPoint(ProtocolConstants.TypeProvidersLoaderPid,
        new TypeProvidersLoader(),
        new TypeProvidersManager(
          new ProvidedNamespacesManager(new ProvidedTypesManager(new ProvidedPropertyInfoManager()))));

      var portValue = args[0];
      var logPath = string.Empty;
      if (args.Length > 1)
      {
        logPath = args[1];
      }

      Console.WriteLine("Try to start with port: " + portValue);
      try
      {
        endPoint.Start(portValue, "TpLog");
      }
      catch (Exception e)
      {
        File.WriteAllText("tplog.txt", e.Message + "\n" + e.StackTrace);
      }
    }
  }
}
