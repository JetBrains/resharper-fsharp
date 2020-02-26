using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public class ProtocolConstants
  {
    public const string TypeProvidersLoaderPid = "TypeProvidersLoaderHost";
    public const string TypeProvidersLoaderFilename = @"C:\Programming\fsharp-support\ReSharper.FSharp\src\TypeProvidersLoader\bin\Debug\net461\JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.exe";
    public static FileSystemPath LogFolder = Util.Logging.Logger.LogFolderPath.Combine("TypeProvidersLoader");
  }
}
