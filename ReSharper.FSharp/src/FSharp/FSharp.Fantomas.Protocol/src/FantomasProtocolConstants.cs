using System;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol
{
  public static class FantomasProtocolConstants
  {
    public const string PROCESS_FILENAME = "JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.dll";
    public const string PARENT_PROCESS_PID_ENV_VARIABLE = "FSHARP_FANTOMAS_PROCESS_PID";
    public static readonly FileSystemPath LogFolder = Logger.LogFolderPath / "Fantomas";
    
    /// Breaking changes:
    /// - fantomas-tool renamed to fantomas 
    /// - Fantomas.dll renamed to Fantomas.Core.dll
    /// - removed FormatSelection/MakeRange APIs
    /// - removed FCS-entities from FormatDocument args
    public static readonly Version Fantomas5Version = Version.Parse("5.0");

    public static string CoreRuntimeConfigFilename =>
      "Fantomas.Host" +
      $".{(PlatformUtil.IsRunningUnderWindows ? "win" : "unix")}.runtimeconfig.json";
  }
}
