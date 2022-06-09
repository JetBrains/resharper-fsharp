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
    private readonly VirtualFileSystemPath mySpecifiedPath;
    protected override string Name => "Fantomas";

    private static readonly FileSystemPath FantomasDirectory =
      typeof(FantomasProcess).Assembly.GetPath().Directory.Parent / "fantomas";
    
    private static readonly FileSystemPath FantomasDllsDirectory = FantomasDirectory / "dll";

    protected override RdFantomasModel CreateModel(Lifetime lifetime, IProtocol protocol) => new(lifetime, protocol);

    protected override FantomasConnection CreateConnection(Lifetime lifetime,
      RdFantomasModel model, IProtocol protocol, StartupOutputWriter outputWriter, int processId,
      Signal<int> processUnexpectedExited) =>
      new(lifetime, model, protocol, outputWriter, processId,
        processUnexpectedExited);

    protected override ProcessStartInfo GetProcessStartInfo(Lifetime lifetime, int port)
    {
      var runtimeConfigPath = FantomasDirectory / FantomasProtocolConstants.CoreRuntimeConfigFilename;
      var launchPath = FantomasDirectory / FantomasProtocolConstants.PROCESS_FILENAME;
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
      };
    }

    protected override bool Shutdown(RdFantomasModel model)
    {
      model.Proto.Scheduler.Queue(() => model.Exit.Fire(Unit.Instance));
      return true;
    }

    public FantomasProcess(Lifetime lifetime, ILogger logger, IShellLocks locks,
      IProcessStartInfoPatcher processInfoPatcher, JetProcessRuntimeRequest request, VirtualFileSystemPath specifiedPath = null)
      : base(lifetime, logger, locks, processInfoPatcher, request, InteractionContext.SolutionContext)
    {
      mySpecifiedPath = specifiedPath;
    }
  }
}
