using JetBrains.Application;
using JetBrains.Application.Components;
using JetBrains.DataFlow;
using JetBrains.Platform.ProjectModel.FSharp.Properties;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Interfaces;
using JetBrains.Util;

namespace JetBrains.Platform.ProjectModel.FSharp
{
  /// <summary>
  /// Should be removed once ReSharper.SDK allows to create new project languages.
  /// </summary>
  [ShellComponent]
  public class FSharpProjectHasPsiService : ProjectHasPsiService, IProjectHasPsiService,
    IHideImplementation<ProjectHasPsiService>
  {
    public FSharpProjectHasPsiService(Lifetime lifetime, ILogger logger) : base(lifetime, logger)
    {
    }

    public new bool ProjectHasPsi(IProject project)
    {
      if (project.ProjectProperties is FSharpProjectProperties) return true;
      return base.ProjectHasPsi(project);
    }
  }
}