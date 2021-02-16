using System;
using FSharp.Compiler;
using JetBrains.Rd.Tasks;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public static class Extensions
  {
    public static string GetLogName(this ExtensionTyping.ProvidedAssembly assembly) =>
      assembly.GetName().Version == null ? "generated assembly" : assembly.FullName;

    public static T ExecuteWithCatch<T>(this TypeProvidersConnection connection, Func<T> func)
    {
      try
      {
        return connection.Execute(func);
      }
      catch (RdFault ex)
      {
        throw new Exception(ex.ReasonMessage, ex);
      }
    }
  }
}
