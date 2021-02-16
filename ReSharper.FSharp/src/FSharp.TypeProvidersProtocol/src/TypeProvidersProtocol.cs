using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol
{
  public static class TypeProvidersProtocol
  {
    public const string TypeProvidersLoaderPid = "TypeProvidersLoaderHost";

    public static string TypeProvidersLoaderFrameworkFilename =>
      "JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.exe";

    public static string TypeProvidersLoaderCoreFilename =>
      "JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.dll";

    public static string CoreRuntimeConfigFilename(int majorVersion) =>
      $"tploader{majorVersion}.{(PlatformUtil.IsRunningUnderWindows ? "win" : "unix")}.runtimeconfig.json";

    public static readonly FileSystemPath LogFolder = Logger.LogFolderPath.Combine("TypeProvidersLoader");
  }
}
