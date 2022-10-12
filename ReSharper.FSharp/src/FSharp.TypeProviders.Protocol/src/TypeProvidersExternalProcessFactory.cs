using JetBrains.Annotations;
using JetBrains.Application.Processes;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Platform.MsBuildHost;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Build;
using JetBrains.ProjectModel.Properties;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  [SolutionComponent]
  public class TypeProvidersExternalProcessFactory
  {
    [NotNull] private readonly ISolutionProcessStartInfoPatcher mySolutionProcessStartInfoPatcher;
    [NotNull] private readonly ILogger myLogger;
    [NotNull] private readonly IShellLocks myShellLocks;
    [NotNull] private readonly ISolutionToolset myToolset;
    [NotNull] private readonly OutputAssemblies myOutputAssemblies;

    public TypeProvidersExternalProcessFactory(
      [NotNull] ISolutionProcessStartInfoPatcher solutionProcessStartInfoPatcher,
      [NotNull] ILogger logger,
      [NotNull] IShellLocks shellLocks,
      [NotNull] ISolutionToolset toolset,
      [NotNull] OutputAssemblies outputAssemblies)
    {
      mySolutionProcessStartInfoPatcher = solutionProcessStartInfoPatcher;
      myLogger = logger;
      myShellLocks = shellLocks;
      myToolset = toolset;
      myOutputAssemblies = outputAssemblies;
    }

    public TypeProvidersExternalProcess Create(Lifetime lifetime,
      [CanBeNull] string requestingProjectOutputPath, bool isInternalMode)
    {
      var sdkVersion = myToolset.GetDotNetCoreToolset()?.Sdk?.Version;

      return new TypeProvidersExternalProcess(lifetime,
        myLogger,
        myShellLocks,
        mySolutionProcessStartInfoPatcher,
        GetProcessRuntime(requestingProjectOutputPath),
        sdkVersion,
        isInternalMode);
    }

    private JetProcessRuntimeRequest GetProcessRuntime([CanBeNull] string requestingProjectOutputPath)
    {
      using var @lock = ReadLockCookie.Create();

      var buildTool = myToolset.GetBuildTool();

      var runtimeType = buildTool!.UseDotNetCoreForLaunch
        ? JetProcessRuntimeType.DotNetCore
        : JetProcessRuntimeType.FullFramework;

      if (requestingProjectOutputPath != null)
      {
        var path = VirtualFileSystemPath.Parse(requestingProjectOutputPath, InteractionContext.SolutionContext);
        var project = myOutputAssemblies.TryGetProjectByOutputAssemblyLocation(path).NotNull();

        foreach (var configuration in project.ProjectProperties.GetActiveConfigurations<IProjectConfiguration>())
          if (configuration.PropertiesCollection.TryGetValue("FscToolExe", out var fscTool) &&
              fscTool is "fsc.exe" or "fsharpc")
            runtimeType = JetProcessRuntimeType.FullFramework;
      }

      var mutator = MsBuildConnectionFactory.GetEnvironmentVariablesMutator(buildTool);

      var runtimeRequest = runtimeType == JetProcessRuntimeType.DotNetCore
        ? JetProcessRuntimeRequest.CreateCore(mutator, true)
        : JetProcessRuntimeRequest.CreateFramework(mutator: mutator);

      return runtimeRequest;
    }
  }
}
