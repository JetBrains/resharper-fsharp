using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Application.Processes;
using JetBrains.Application.Threading;
using JetBrains.Core;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Rd;
using JetBrains.ReSharper.Plugins.FSharp.Fantomas.Client;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol
{
  public class FantomasProcess : ProtocolExternalProcess<RdFantomasModel, FantomasConnection>
  {
    private readonly string myVersion;
    private readonly VirtualFileSystemPath mySpecifiedPath;
    protected override string Name => "Fantomas";

    private static readonly FileSystemPath FantomasHostDirectory =
      typeof(FantomasProcess).Assembly.GetPath().Directory.Parent / "fantomas";

    private static readonly FileSystemPath FantomasDllsDirectory = FantomasHostDirectory;

    protected override RdFantomasModel CreateModel(Lifetime lifetime, IProtocol protocol) => new(lifetime, protocol);

    protected override FantomasConnection CreateConnection(Lifetime lifetime,
      RdFantomasModel model, IProtocol protocol, StartupOutputWriter outputWriter, int processId,
      Signal<int> processUnexpectedExited) =>
      new(lifetime, model, protocol, outputWriter, processId,
        processUnexpectedExited);

    protected override ProcessStartInfo GetProcessStartInfo(Lifetime lifetime, int port)
    {
      var runtimeConfigPath = FantomasHostDirectory / FantomasProtocolConstants.CoreRuntimeConfigFilename;
      var launchPath =
        // because the internal SDK produces .exe instead of .dll
        FantomasHostDirectory / (FantomasProtocolConstants.PROCESS_FILENAME_WITHOUT_EXTENSION + ".exe")
          is { ExistsFile: true } exeFile
            ? exeFile
            : FantomasHostDirectory / (FantomasProtocolConstants.PROCESS_FILENAME_WITHOUT_EXTENSION + ".dll");
      var dotnetArgs = $"--runtimeconfig \"{runtimeConfigPath}\"";

      Assertion.Assert(launchPath.ExistsFile, $"can't find '{launchPath}'");
      Assertion.Assert(runtimeConfigPath.ExistsFile, $"can't find '{runtimeConfigPath.FullPath}'");

      return new ProcessStartInfo
      {
        Arguments =
          $"{dotnetArgs} \"{launchPath.FullPath}\" {port} \"{FantomasProtocolConstants.LogFolder.Combine($"{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_ffff}.log")}\"",
        FileName = "exec"
      };
    }

    protected override IDictionary<string, string> GetAdditionalProcessEnvVars()
    {
      return new Dictionary<string, string>
      {
        {
          "RIDER_PLUGIN_ADDITIONAL_PROBING_PATHS",
          Environment.GetEnvironmentVariable("RIDER_PLUGIN_ADDITIONAL_PROBING_PATHS")
        },
        {
          FantomasProtocolConstants.PARENT_PROCESS_PID_ENV_VARIABLE,
          Process.GetCurrentProcess().Id.ToString()
        },
        {
          "FSHARP_FANTOMAS_ASSEMBLIES_PATH",
          mySpecifiedPath?.FullPath ?? FantomasDllsDirectory.FullPath
        },
        {
          "FSHARP_FANTOMAS_VERSION", myVersion
        }
      };
    }

    protected override bool Shutdown(RdFantomasModel model)
    {
      model.TryGetProto().NotNull().Scheduler.Queue(() => model.Exit.Fire(Unit.Instance));
      return true;
    }

    public FantomasProcess(Lifetime processLifetime, ILogger logger, IShellLocks locks,
      IProcessStartInfoPatcher processInfoPatcher, JetProcessRuntimeRequest request, string version,
      VirtualFileSystemPath specifiedPath = null)
      : base(processLifetime, logger, locks, processInfoPatcher, request, InteractionContext.SolutionContext)
    {
      myVersion = version;
      mySpecifiedPath = specifiedPath;
    }
  }
}
