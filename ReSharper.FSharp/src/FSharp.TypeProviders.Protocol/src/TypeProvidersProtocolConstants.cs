using System;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  public static class TypeProvidersProtocolConstants
  {
    public const string TypeProvidersHostPid = "TypeProvidersHost";

    public static string TypeProvidersHostFrameworkFilename =>
      "JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.exe";

    public static string TypeProvidersHostCoreFilename =>
      "JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.NetCore.dll";

    public static string CoreRuntimeConfigFilename(int majorVersion) =>
      "tploader." +
      majorVersion switch
      {
        3 => "netcoreapp31",
        >= 5 and <= 7 => $"net{majorVersion}",
        var x => throw new InvalidOperationException($"Wrong runtime version '{x}'")
      } +
      $".{(PlatformUtil.IsRunningUnderWindows ? "win" : "unix")}.runtimeconfig.json";

    public static readonly FileSystemPath LogFolder = Logger.LogFolderPath / "TypeProvidersHost";
  }
}
