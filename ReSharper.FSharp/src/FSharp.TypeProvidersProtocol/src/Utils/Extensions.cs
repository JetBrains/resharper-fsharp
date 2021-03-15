using FSharp.Compiler;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public static class Extensions
  {
    public static string GetLogName(this ExtensionTyping.ProvidedAssembly assembly) =>
      assembly.GetName().Version == null ? "generated assembly" : assembly.FullName;
  }
}
