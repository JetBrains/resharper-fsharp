using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.ProjectModel.FSharp.ProjectProperties;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.PsiGen.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Threading;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.ProjectOptions
{
  [Obsolete]
  public class FSharpProjectOptionsProviderOld
  {
    private readonly ISolution mySolution;
    private readonly IDaemonSuspender myDaemonSuspender;
    private readonly FSharpProjectOptionsBuilderOld myProjectOptionsBuilder;
    private readonly FSharpCheckerServiceOld myFSharpCheckerService;
    private readonly GroupingEvent myUpdateEvent;
    private IDisposable myDaemonSuspendedDisposable;

    private readonly HashSet<IProject> myProjectsToUpdate = new HashSet<IProject>();

    public FSharpProjectOptionsProviderOld(Lifetime lifetime, ISolution solution, IDaemonSuspender daemonSuspender,
      ChangeManager changeManager, FSharpCheckerServiceOld fSharpCheckerService,
      FSharpProjectOptionsBuilderOld projectOptionsBuilder)
    {
      mySolution = solution;
      myDaemonSuspender = daemonSuspender;
      myProjectOptionsBuilder = projectOptionsBuilder;
      myFSharpCheckerService = fSharpCheckerService;
      myUpdateEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "updateFSharpProjectsOptions",
        TimeSpan.FromMilliseconds(500), Rgc.Guarded, DoUpdateProjects);

//      changeManager.Changed2.Advise(lifetime, ProcessChange);
    }

    private void AddReferencingProjects(IProject project, HashSet<IProject> projects)
    {
      if (projects.Contains(project)) return;
      projects.Add(project);
      Assertion.AssertNotNull(project, "project != null");
      foreach (var referencingProject in project.GetReferencingProjects(project.GetCurrentTargetFrameworkId()))
        if (referencingProject.ProjectProperties is FSharpProjectProperties)
          AddReferencingProjects(referencingProject, projects);
    }

    private void DoUpdateProjects()
    {
      // todo: reparse files, reprocess caches (e.g. when change configuration)

      mySolution.Locks.AssertMainThread();

      using (ReadLockCookie.Create())
      {
        var referencingProjects = new HashSet<IProject>();
        foreach (var projectGuid in myProjectsToUpdate)
          AddReferencingProjects(projectGuid, referencingProjects);
        myProjectsToUpdate.addAll(referencingProjects);

        var projectsOptions = new Dictionary<IProject, FSharpProjectOptions>();
        foreach (var project in myProjectsToUpdate)
          projectsOptions.Add(project, myProjectOptionsBuilder.BuildWithoutReferencedProjects(project));

        foreach (var projectOptions in myProjectOptionsBuilder.AddReferencedProjects(projectsOptions))
        {
          var project = projectOptions.Key;
          var options = projectOptions.Value;
          myFSharpCheckerService.AddProject(project.Guid, options, myProjectOptionsBuilder.GetDefines(project));
        }
        myProjectsToUpdate.Clear();

        if (myUpdateEvent.IsWaiting())
          return;

        myDaemonSuspendedDisposable.Dispose();
        myDaemonSuspendedDisposable = null;
      }
    }

    private void ProcessChange(ChangeEventArgs obj)
    {
      var change = obj.ChangeMap.GetChange<ProjectModelChange>(mySolution);
      if (change == null) return;
      ProcessChange(change);
    }

    private void ProcessChange(ProjectModelChange change)
    {
      var project = change.ProjectModelElement as IProject;
      if (project != null && project.ProjectProperties.ProjectKind != ProjectKind.SOLUTION_FOLDER)
      {
        if (IsApplicable(project.ProjectProperties))
        {
          if (change.IsRemoved)
          {
            myFSharpCheckerService.RemoveProject(project.Guid); // todo: referenced projects
            return;
          }
          if (myDaemonSuspendedDisposable == null)
            myDaemonSuspendedDisposable = myDaemonSuspender.Suspend();
          myProjectsToUpdate.Add(project);
          myUpdateEvent.FireIncoming();
        }
        return;
      }
      foreach (var projectModelChange in change.GetChildren())
        ProcessChange(projectModelChange);
    }

    private static bool IsApplicable([NotNull] IProjectProperties properties)
    {
      var fsProjProperties = properties as FSharpProjectProperties;
      if (fsProjProperties != null)
        return true;

      var coreProjProperties = properties as ProjectKCSharpProjectProperties;
      return coreProjProperties != null &&
             coreProjProperties.ProjectTypeGuids.Contains(FSharpProjectPropertiesFactory.FSharpProjectTypeGuid);
    }
  }
}