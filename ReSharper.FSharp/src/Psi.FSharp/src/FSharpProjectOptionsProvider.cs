using System;
using System.Collections.Generic;
using JetBrains.Application.changes;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.ProjectModel.FSharp.Properties;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.PsiGen.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Threading;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp
{
  [SolutionComponent]
  public class FSharpProjectOptionsProvider
  {
    private readonly ISolution mySolution;
    private readonly FSharpProjectOptionsBuilder myProjectOptionsBuilder;
    private readonly FSharpCheckerService myFSharpCheckerService;
    private readonly GroupingEvent myUpdateEvent;
    private readonly HashSet<Guid> myProjectsToUpdateGuids = new HashSet<Guid>();

    public FSharpProjectOptionsProvider(Lifetime lifetime, ISolution solution, ChangeManager changeManager,
      FSharpCheckerService fSharpCheckerService, FSharpProjectOptionsBuilder projectOptionsBuilder)
    {
      mySolution = solution;
      myProjectOptionsBuilder = projectOptionsBuilder;
      myFSharpCheckerService = fSharpCheckerService;
      myUpdateEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "updateFSharpProjects",
        TimeSpan.FromMilliseconds(100), Rgc.Guarded,
        DoUpdateProjects); // todo: wait in FSharpCheckerService if new file added and options are not ready

      changeManager.Changed2.Advise(lifetime, ProcessChange);
    }

    private void AddReferencingProjects(Guid projectGuid, HashSet<Guid> projectGuids)
    {
      if (projectGuids.Contains(projectGuid)) return;
      projectGuids.Add(projectGuid);
      var project = mySolution.GetProjectByGuid(projectGuid);
      Assertion.AssertNotNull(project, "project != null");
      foreach (var referencingProject in project.GetReferencingProjects(project.GetCurrentTargetFrameworkId()))
        if (referencingProject.ProjectProperties is FSharpProjectProperties)
          AddReferencingProjects(referencingProject.Guid, projectGuids);
    }

    private void DoUpdateProjects()
    {
      // todo: reparse files, reprocess caches

      mySolution.Locks.AssertMainThread();

      using (ReadLockCookie.Create())
      {
        var referencingProjects = new HashSet<Guid>();
        foreach (var projectGuid in myProjectsToUpdateGuids)
          AddReferencingProjects(projectGuid, referencingProjects);
        myProjectsToUpdateGuids.addAll(referencingProjects);

        var projectsOptions = new Dictionary<Guid, FSharpProjectOptions>();
        foreach (var projectGuid in myProjectsToUpdateGuids)
          projectsOptions.Add(projectGuid, myProjectOptionsBuilder.BuildWithoutReferencedProjects(projectGuid));

        foreach (var projectOptions in myProjectOptionsBuilder.AddReferencedProjects(projectsOptions))
        {
          var projectGuid = projectOptions.Key;
          var options = projectOptions.Value;
          myFSharpCheckerService.AddProject(projectGuid, options, myProjectOptionsBuilder.GetDefines(projectGuid));
        }
        myProjectsToUpdateGuids.Clear();
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
        if (project.ProjectProperties is FSharpProjectProperties)
        {
          if (change.IsRemoved)
          {
            myFSharpCheckerService.RemoveProject(project.Guid);
            return;
          }
          myProjectsToUpdateGuids.Add(project.Guid);
          myUpdateEvent.FireIncoming();
        }
        return;
      }
      foreach (var projectModelChange in change.GetChildren())
        ProcessChange(projectModelChange);
    }
  }
}