using JetBrains.Annotations;
using JetBrains.Application.Processes;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.Platform.MsBuildHost;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  public interface IProjectsRequiringFrameworkVisitor
  {
    bool RequiresNetFramework(string projectOutputPath);
  }


  [SolutionComponent]
  public class TypeProvidersExternalProcessFactory
  {
    [NotNull] private readonly ISolution mySolution;
    [NotNull] private readonly ISolutionProcessStartInfoPatcher mySolutionProcessStartInfoPatcher;
    [NotNull] private readonly ILogger myLogger;
    [NotNull] private readonly IShellLocks myShellLocks;
    [NotNull] private readonly ISolutionToolset myToolset;

    public TypeProvidersExternalProcessFactory(
      [NotNull] ISolution solution,
      [NotNull] ISolutionProcessStartInfoPatcher solutionProcessStartInfoPatcher,
      [NotNull] ILogger logger,
      [NotNull] IShellLocks shellLocks,
      [NotNull] ISolutionToolset toolset)
    {
      mySolution = solution;
      mySolutionProcessStartInfoPatcher = solutionProcessStartInfoPatcher;
      myLogger = logger;
      myShellLocks = shellLocks;
      myToolset = toolset;
    }

    public TypeProvidersExternalProcess Create(Lifetime lifetime,
      [CanBeNull] string requestingProjectOutputPath, bool isInternalMode)
    {
      var toolset = myToolset.GetDotNetCoreToolset();

      return new TypeProvidersExternalProcess(lifetime,
        myLogger,
        myShellLocks,
        mySolutionProcessStartInfoPatcher,
        GetProcessRuntime(requestingProjectOutputPath),
        toolset,
        isInternalMode);
    }

    private JetProcessRuntimeRequest GetProcessRuntime([CanBeNull] string requestingProjectOutputPath)
    {
      var projectVisitor = mySolution.GetComponent<IProjectsRequiringFrameworkVisitor>();
      var buildTool = myToolset.GetBuildTool();

      var runtimeType = buildTool!.UseDotNetCoreForLaunch
        ? JetProcessRuntimeType.DotNetCore
        : JetProcessRuntimeType.FullFramework;

      if (requestingProjectOutputPath != null && projectVisitor.RequiresNetFramework(requestingProjectOutputPath))
        runtimeType = JetProcessRuntimeType.FullFramework;

      var mutator = MsBuildConnectionFactory.GetEnvironmentVariablesMutator(buildTool);

      var runtimeRequest = runtimeType == JetProcessRuntimeType.DotNetCore
        ? JetProcessRuntimeRequest.CreateCore(mutator, true)
        : JetProcessRuntimeRequest.CreateFramework(mutator: mutator);

      return runtimeRequest;
    }
  }
}
