using JetBrains.Annotations;
using JetBrains.Application.Processes;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol
{
  [SolutionComponent]
  public class TypeProvidersLoaderExternalProcessFactory
  {
    [NotNull] private readonly ISolutionProcessStartInfoPatcher mySolutionProcessStartInfoPatcher;
    [NotNull] private readonly ILogger myLogger;
    [NotNull] private readonly IShellLocks myShellLocks;

    public TypeProvidersLoaderExternalProcessFactory(
      [NotNull] ISolutionProcessStartInfoPatcher solutionProcessStartInfoPatcher,
      [NotNull] ILogger logger,
      [NotNull] IShellLocks shellLocks)
    {
      mySolutionProcessStartInfoPatcher = solutionProcessStartInfoPatcher;
      myLogger = logger;
      myShellLocks = shellLocks;
    }

    public virtual OutOfProcessTypeProvidersProtocol Create(Lifetime lifetime)
    {
      var jetProcessRuntimeRequest = JetProcessRuntimeRequest.CreateFramework();
      return new OutOfProcessTypeProvidersProtocol(lifetime,
        ProtocolConstants.TypeProvidersLoaderFilename,
        ProtocolConstants.TypeProvidersLoaderPid,
        myLogger,
        myShellLocks,
        mySolutionProcessStartInfoPatcher,
        jetProcessRuntimeRequest);
    }
  }
}
