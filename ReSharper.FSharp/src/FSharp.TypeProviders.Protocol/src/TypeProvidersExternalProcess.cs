using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Application.Processes;
using JetBrains.Application.Threading;
using JetBrains.Core;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.ProjectModel.BuildTools;
using JetBrains.Rd;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  public class
    TypeProvidersExternalProcess : ProtocolExternalProcess<RdFSharpTypeProvidersModel, TypeProvidersConnection>
  {
    private readonly JetProcessRuntimeRequest myRequest;
    private readonly DotNetCoreToolset myToolset;
    private readonly bool myIsInternalMode;
    private Lifetime myLifetime;

    protected override string Name => "Out-of-Process TypeProviders";

    private static readonly FileSystemPath TypeProvidersDirectory =
      typeof(TypeProvidersExternalProcess).Assembly.GetPath().Directory.Parent / "typeProviders";

    protected override RdFSharpTypeProvidersModel CreateModel(Lifetime lifetime, IProtocol protocol) =>
      new(lifetime, protocol);

    protected override TypeProvidersConnection CreateConnection(Lifetime lifetime,
      RdFSharpTypeProvidersModel model, IProtocol protocol, StartupOutputWriter outputWriter, int processId,
      Signal<int> processUnexpectedExited)
    {
      myLifetime = lifetime;
      return new TypeProvidersConnection(lifetime, model, protocol, outputWriter, processId,
        processUnexpectedExited);
    }

    protected override ProcessStartInfo GetProcessStartInfo(Lifetime lifetime, int port) =>
      myRequest.RuntimeType == JetProcessRuntimeType.DotNetCore
        ? GetCoreProcessStartInfo(port, TypeProvidersDirectory)
        : GetFrameworkProcessStartInfo(port, TypeProvidersDirectory);

    private ProcessStartInfo GetCoreProcessStartInfo(int port, FileSystemPath basePath)
    {
      var sdkMajorVersion = myToolset.Sdk.NotNull().Version.Major;
      var sharedFrameworkName = PlatformUtil.IsRunningUnderWindows
        ? "Microsoft.WindowsDesktop.App"
        : "Microsoft.NETCore.App";

      var sharedFrameworkVersions = myToolset.Cli.Runtimes
        .FirstOrDefault(t => t.Name == sharedFrameworkName)
        .NotNull($"{sharedFrameworkName} should exists");

      var majorVersions = sharedFrameworkVersions.Versions
        .Where(v => v.Major == sdkMajorVersion)
        .ToList();
      Assertion.Assert(majorVersions.Any(),
        $"{sharedFrameworkName} shoul contains at least one {sdkMajorVersion} major version");

      var versionToRun = majorVersions.Max();

      var runtimeConfigPath = basePath / TypeProvidersProtocolConstants.CoreRuntimeConfigFilename;
      var fileSystemPath = basePath / TypeProvidersProtocolConstants.TypeProvidersHostCoreFilename;
      var dotnetArgs = $"--fx-version {versionToRun} --runtimeconfig \"{runtimeConfigPath}\"";
      Assertion.Assert(fileSystemPath.ExistsFile, $"can't find '{fileSystemPath.FullPath}'");
      Assertion.Assert(runtimeConfigPath.ExistsFile, $"can't find '{runtimeConfigPath.FullPath}'");

      var processStartInfo = new ProcessStartInfo
      {
        Arguments =
          $"{dotnetArgs} \"{fileSystemPath.FullPath}\" {port} \"{TypeProvidersProtocolConstants.LogFolder.Combine($"{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_ffff}.log")}\"",
        FileName = "exec"
      };

      return processStartInfo;
    }

    private static ProcessStartInfo GetFrameworkProcessStartInfo(int port, FileSystemPath basePath)
    {
      var fileSystemPath = basePath / TypeProvidersProtocolConstants.TypeProvidersHostFrameworkFilename;
      Assertion.Assert(fileSystemPath.ExistsFile, $"can't find '{fileSystemPath.FullPath}'");

      var processStartInfo = new ProcessStartInfo
      {
        Arguments =
          $"{port} \"{TypeProvidersProtocolConstants.LogFolder.Combine($"{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_ffff}.log")}\"",
        FileName = fileSystemPath.FullPath
      };

      return processStartInfo;
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
          TypeProvidersProtocolConstants.TypeProvidersHostPid,
          Process.GetCurrentProcess().Id.ToString()
        },
        {
          "RESHARPER_INTERNAL_MODE", myIsInternalMode.ToString()
        }
      };
    }

    protected override bool Shutdown(RdFSharpTypeProvidersModel model)
    {
      model.Proto.Scheduler.Queue(() => model.RdTypeProviderProcessModel.Kill.Start(myLifetime, Unit.Instance));
      return true;
    }

    public TypeProvidersExternalProcess(Lifetime lifetime, ILogger logger, IShellLocks locks,
      IProcessStartInfoPatcher processInfoPatcher, JetProcessRuntimeRequest request, DotNetCoreToolset toolset,
      bool isInternalMode)
      : base(lifetime, logger, locks, processInfoPatcher, request, InteractionContext.SolutionContext)
    {
      myRequest = request;
      myToolset = toolset;
      myIsInternalMode = isInternalMode;
    }
  }
}
