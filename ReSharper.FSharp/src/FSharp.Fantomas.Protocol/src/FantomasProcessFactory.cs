using JetBrains.Annotations;
using JetBrains.Application.Processes;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol
{
  [SolutionComponent]
  public class FantomasProcessFactory
  {
    [NotNull] private readonly ISolutionProcessStartInfoPatcher mySolutionProcessStartInfoPatcher;
    [NotNull] private readonly ILogger myLogger;
    [NotNull] private readonly IShellLocks myShellLocks;

    public FantomasProcessFactory(
      [NotNull] ISolutionProcessStartInfoPatcher solutionProcessStartInfoPatcher,
      [NotNull] ILogger logger,
      [NotNull] IShellLocks shellLocks)
    {
      mySolutionProcessStartInfoPatcher = solutionProcessStartInfoPatcher;
      myLogger = logger;
      myShellLocks = shellLocks;
    }

    public FantomasProcess Create(Lifetime lifetime, VirtualFileSystemPath path = null)
    {
      return new FantomasProcess(lifetime,
        myLogger,
        myShellLocks,
        mySolutionProcessStartInfoPatcher,
        JetProcessRuntimeRequest.CreateCore(), path);
    }
  }
}
