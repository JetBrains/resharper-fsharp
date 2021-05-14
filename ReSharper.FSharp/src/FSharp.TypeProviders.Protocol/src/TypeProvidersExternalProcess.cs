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
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using JetBrains.Util;
using NuGet.Versioning;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  public class
    TypeProvidersExternalProcess : ProtocolExternalProcess<RdFSharpTypeProvidersModel, TypeProvidersConnection>
  {
    private readonly JetProcessRuntimeRequest myRequest;
    private readonly NuGetVersion myNuGetVersion;
    private Lifetime myLifetime;
    protected override string Name => "Out-of-Process TypeProviders";

    protected override RdFSharpTypeProvidersModel CreateModel(Lifetime lifetime, IProtocol protocol) =>
      new RdFSharpTypeProvidersModel(lifetime, protocol);

    protected override TypeProvidersConnection CreateConnection(Lifetime lifetime,
      RdFSharpTypeProvidersModel model, IProtocol protocol, StartupOutputWriter outputWriter, int processId,
      Signal<int> processUnexpectedExited)
    {
      myLifetime = lifetime;
      return new TypeProvidersConnection(lifetime, model, protocol, outputWriter, processId,
        processUnexpectedExited);
    }

    protected override ProcessStartInfo GetProcessStartInfo(int port)
    {
      var basePath = GetType().Assembly.GetPath().Directory;

      return myRequest.RuntimeType == JetProcessRuntimeType.DotNetCore
        ? GetCoreProcessStartInfo(port, basePath)
        : GetFrameworkProcessStartInfo(port, basePath);
    }

    private ProcessStartInfo GetCoreProcessStartInfo(int port, FileSystemPath basePath)
    {
      var sdkMajorVersion = myNuGetVersion.Major.Clamp(3, 6);
      var runtimeConfigPath = basePath.Combine(TypeProvidersProtocolConstants.CoreRuntimeConfigFilename(sdkMajorVersion));
      var fileSystemPath = basePath.Combine(TypeProvidersProtocolConstants.TypeProvidersHostCoreFilename);
      var dotnetArgs = $"--runtimeconfig \"{runtimeConfigPath}\"";

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
      var fileSystemPath = basePath.Combine(TypeProvidersProtocolConstants.TypeProvidersHostFrameworkFilename);
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
      return new Dictionary<string, string>()
      {
        {
          "RIDER_PLUGIN_ADDITIONAL_PROBING_PATHS",
          Environment.GetEnvironmentVariable("RIDER_PLUGIN_ADDITIONAL_PROBING_PATHS")
        },
        {
          TypeProvidersProtocolConstants.TypeProvidersHostPid,
          Process.GetCurrentProcess().Id.ToString()
        }
      };
    }

    protected override bool Shutdown(RdFSharpTypeProvidersModel model)
    {
      model.Proto.Scheduler.Queue(() => model.RdTypeProviderProcessModel.Kill.Start(myLifetime, Unit.Instance));
      return true;
    }

    public TypeProvidersExternalProcess(Lifetime lifetime, ILogger logger, IShellLocks locks,
      IProcessStartInfoPatcher processInfoPatcher, JetProcessRuntimeRequest request, NuGetVersion nuGetVersion)
      : base(lifetime, logger, locks, processInfoPatcher, request)
    {
      myRequest = request;
      myNuGetVersion = nuGetVersion;
    }
  }
}
