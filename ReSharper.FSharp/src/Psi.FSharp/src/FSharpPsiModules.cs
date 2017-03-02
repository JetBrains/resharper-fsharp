using JetBrains.Application;
using JetBrains.Application.changes;
using JetBrains.Application.Components;
using JetBrains.DataFlow;
using JetBrains.Platform.ProjectModel.FSharp.Properties;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Build;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp
{
  /// <summary>
  /// Overrides non-extensible API in ReSharper.
  /// Should be removed when possible to override IsSourceProject differrent way.
  /// </summary>
  [PsiComponent]
  public class FSharpPsiModules : PsiModules, IPsiModules, IHideImplementation<IPsiModules>
  {
    public FSharpPsiModules(Lifetime lifetime, ILogger logger, IShellLocks locks, ISolution solution,
      ChangeManager changeManager, PsiProjectFileTypeCoordinator psiProjectFileTypeCoordinator,
      ProjectPsiModuleFactory projectModuleFactory, AssemblyPsiModuleFactory assemblyModuleFactory,
      OutputAssemblies outputAssembliesCache, IViewable<IPsiModuleFactory> factories,
      ISolutionLoadTasksScheduler loadTasksScheduler, IPsiSourceFileSorter sourceFileSorter)
      : base(lifetime, logger, locks, solution, changeManager, psiProjectFileTypeCoordinator, projectModuleFactory,
        assemblyModuleFactory, outputAssembliesCache, factories, loadTasksScheduler, sourceFileSorter)
    {
    }

    public new bool IsSourceProject(IProject project)
    {
      return project.ProjectProperties is FSharpProjectProperties || base.IsSourceProject(project);
    }
  }
}