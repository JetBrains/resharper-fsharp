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
    protected override string Name => "Fantomas";

    private static readonly FileSystemPath FantomasDirectory =
      typeof(FantomasProcess).Assembly.GetPath().Directory.Parent.Combine("fantomas");

    protected override RdFantomasModel CreateModel(Lifetime lifetime, IProtocol protocol) =>
      new RdFantomasModel(lifetime, protocol);

    protected override FantomasConnection CreateConnection(Lifetime lifetime,
      RdFantomasModel model, IProtocol protocol, StartupOutputWriter outputWriter, int processId,
      Signal<int> processUnexpectedExited) =>
      new FantomasConnection(lifetime, model, protocol, outputWriter, processId,
        processUnexpectedExited);

    protected override ProcessStartInfo GetProcessStartInfo(int port)
    {
      var launchPath = FantomasDirectory.Combine(FantomasProtocolConstants.PROCESS_FILENAME);
      Assertion.Assert(launchPath.ExistsFile, $"can't find '{launchPath}'");

      return new ProcessStartInfo
      {
        Arguments =
          $"{port} \"{FantomasProtocolConstants.LogFolder.Combine($"{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_ffff}.log")}\"",
        FileName = launchPath.FullPath
      };
    }

    protected override IDictionary<string, string> GetAdditionalProcessEnvVars()
    {
      return new Dictionary<string, string>()
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
          FantomasDirectory.FullPath
        },
      };
    }

    protected override bool Shutdown(RdFantomasModel model)
    {
      model.Proto.Scheduler.Queue(() => model.Exit.Fire(Unit.Instance));
      return true;
    }

    public FantomasProcess(Lifetime lifetime, ILogger logger, IShellLocks locks,
      IProcessStartInfoPatcher processInfoPatcher, JetProcessRuntimeRequest request)
      : base(lifetime, logger, locks, processInfoPatcher, request)
    {
    }
  }
}
