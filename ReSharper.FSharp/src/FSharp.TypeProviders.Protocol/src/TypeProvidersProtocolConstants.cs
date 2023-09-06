using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  public static class TypeProvidersProtocolConstants
  {
    public const string TypeProvidersHostPid = "TypeProvidersHost";
    public const string TraceScenario = "JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host";

    public static string TypeProvidersHostFrameworkFilename =>
      "JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.exe";

    public static string TypeProvidersHostCoreFilename =>
      "JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.NetCore.dll";

    public static string CoreRuntimeConfigFilename =>
      $"tploader.{(PlatformUtil.IsRunningUnderWindows ? "win" : "unix")}.runtimeconfig.json";

    public static readonly FileSystemPath LogFolder = Logger.LogFolderPath / "TypeProvidersHost";
  }
}
