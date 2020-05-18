using System;
using JetBrains.Annotations;
using JetBrains.Application.Processes;
using JetBrains.Application.Threading;
using JetBrains.Core;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Rd;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol
{
  public class OutOfProcessTypeProvidersProtocol : ConfiguredProtocolExternalProcess<RdFSharpTypeProvidersLoaderModel>
  {
    public OutOfProcessTypeProvidersProtocol(
      Lifetime lifetime,
      [NotNull] string processFileName,
      [NotNull] string parentProcessPidEnvironmentVariable,
      [NotNull] ILogger logger,
      [NotNull] IShellLocks shellLocks,
      [NotNull] IProcessStartInfoPatcher processInfoPatcher,
      [NotNull] JetProcessRuntimeRequest request)
      : base(lifetime, processFileName, parentProcessPidEnvironmentVariable, ProtocolConstants.LogFolder, logger,
        shellLocks,
        processInfoPatcher, request)
    {
    }

    protected override void OnShutdownRequested(Lifetime remainingLifetime,
      RdFSharpTypeProvidersLoaderModel protocolModel)
    {
      protocolModel?.Kill.Sync(Unit.Instance);
    }

    protected override void CreateModel(Lifetime processLifetime, IProtocol protocol,
      Action<RdFSharpTypeProvidersLoaderModel> onInitialized)
    {
      onInitialized(new RdFSharpTypeProvidersLoaderModel(processLifetime, protocol));
    }

    protected override string Name => "Out-of-Process TypeProviders protocol";
  }
}
