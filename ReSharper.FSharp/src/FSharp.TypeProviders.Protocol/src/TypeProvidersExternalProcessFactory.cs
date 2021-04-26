using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Processes;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.Platform.MsBuildHost;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.NuGet.Packaging;
using JetBrains.RdBackend.Common.Features.Toolset;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  [SolutionComponent]
  public class TypeProvidersExternalProcessFactory
  {
    [NotNull] private readonly ISolutionProcessStartInfoPatcher mySolutionProcessStartInfoPatcher;
    [NotNull] private readonly ILogger myLogger;
    [NotNull] private readonly IShellLocks myShellLocks;
    [NotNull] private readonly ISolution mySolution;
    [NotNull] private readonly IRiderSolutionToolset myToolset;

    public TypeProvidersExternalProcessFactory(
      [NotNull] ISolutionProcessStartInfoPatcher solutionProcessStartInfoPatcher,
      [NotNull] ILogger logger,
      [NotNull] IShellLocks shellLocks,
      [NotNull] ISolution solution,
      [NotNull] IRiderSolutionToolset toolset)
    {
      mySolutionProcessStartInfoPatcher = solutionProcessStartInfoPatcher;
      myLogger = logger;
      myShellLocks = shellLocks;
      mySolution = solution;
      myToolset = toolset;
    }

    public TypeProvidersExternalProcess Create(Lifetime lifetime)
    {
      var sdkVersion = myToolset.GetDotNetCoreToolset()?.Sdk?.Version;

      return new TypeProvidersExternalProcess(lifetime,
        myLogger,
        myShellLocks,
        mySolutionProcessStartInfoPatcher,
        GetProcessRuntime(),
        sdkVersion);
    }

    private JetProcessRuntimeRequest GetProcessRuntime()
    {
      var packageReferenceTracker = mySolution.GetComponent<NuGetPackageReferenceTracker>();

      var installedPackages = packageReferenceTracker.GetAllInstalledPackages();
      var containsLegacyCompiler = installedPackages.Any(x => x.PackageIdentity.Id == "FSharp.Compiler.Tools");

      var buildTool = myToolset.GetBuildTool();
      var mutator = MsBuildConnectionFactory.GetEnvironmentVariablesMutator(buildTool);

      var runtimeRequest = buildTool!.UseDotNetCoreForLaunch && !containsLegacyCompiler
        ? JetProcessRuntimeRequest.CreateCore(mutator, true)
        : JetProcessRuntimeRequest.CreateFramework(mutator: mutator);

      return runtimeRequest;
    }
  }
}
