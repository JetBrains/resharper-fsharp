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
using JetBrains.Rider.FSharp.ExternalFormatter.Server;
using JetBrains.Util;

namespace FSharp.ExternalFormatter.Protocol
{
  public class
    ExternalFormatterProcess : ProtocolExternalProcess<RdFSharpExternalFormatterModel, ExternalFormatterConnection>
  {
    protected override string Name => "External Formatter";

    protected override RdFSharpExternalFormatterModel CreateModel(Lifetime lifetime, IProtocol protocol) =>
      new RdFSharpExternalFormatterModel(lifetime, protocol);

    protected override ExternalFormatterConnection CreateConnection(Lifetime lifetime,
      RdFSharpExternalFormatterModel model, IProtocol protocol, StartupOutputWriter outputWriter, int processId,
      Signal<int> processUnexpectedExited) =>
      new ExternalFormatterConnection(lifetime, model, protocol, outputWriter, processId,
        processUnexpectedExited);

    protected override ProcessStartInfo GetProcessStartInfo(int port)
    {
      var launchPath = GetType().Assembly.GetPath().Directory.Combine(ProtocolConstants.PROCESS_FILENAME);
      Assertion.Assert(launchPath.ExistsFile, $"can't find '{ProtocolConstants.PROCESS_FILENAME}'");

      return new ProcessStartInfo
      {
        Arguments =
          $"{port} \"{ProtocolConstants.LogFolder.Combine($"{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_ffff}.log")}\"",
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
          ProtocolConstants.PARENT_PROCESS_PID_ENV_VARIABLE,
          Process.GetCurrentProcess().Id.ToString()
        },
      };
    }

    protected override bool Shutdown(RdFSharpExternalFormatterModel model)
    {
      model.Proto.Scheduler.Queue(() => model.Exit.Fire(Unit.Instance));
      return true;
    }

    public ExternalFormatterProcess(Lifetime lifetime, ILogger logger, IShellLocks locks,
      IProcessStartInfoPatcher processInfoPatcher, JetProcessRuntimeRequest request)
      : base(lifetime, logger, locks, processInfoPatcher, request)
    {
    }
  }
}
