using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  public static class ProtocolConstants
  {
    public const string TypeProvidersHostPid = "TypeProvidersHost";

    public static string TypeProvidersHostFrameworkFilename =>
      "JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.exe";

    public static string TypeProvidersHostCoreFilename =>
      "JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Core.dll";

    public static string CoreRuntimeConfigFilename(int majorVersion) =>
      $"tploader{majorVersion}.{(PlatformUtil.IsRunningUnderWindows ? "win" : "unix")}.runtimeconfig.json";

    public static readonly FileSystemPath LogFolder = Logger.LogFolderPath.Combine("TypeProvidersHost");
  }
}
