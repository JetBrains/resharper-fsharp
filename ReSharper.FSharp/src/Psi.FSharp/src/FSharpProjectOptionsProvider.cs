using System;
using System.Collections.Generic;
using JetBrains.Application.changes;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.ProjectModel.FSharp.Properties;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.PsiGen.Util;
using JetBrains.Threading;
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
    private readonly HashSet<IProject> myProjectsToUpdate = new HashSet<IProject>();

    public FSharpProjectOptionsProvider(Lifetime lifetime, ISolution solution, ChangeManager changeManager,
      FSharpCheckerService fSharpCheckerService, FSharpProjectOptionsBuilder projectOptionsBuilder)
    {
      mySolution = solution;
      myProjectOptionsBuilder = projectOptionsBuilder;
      myFSharpCheckerService = fSharpCheckerService;
      myUpdateEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "updateFSharpProjects",
        TimeSpan.FromMilliseconds(500), Rgc.Guarded, DoUpdateProjects);

      changeManager.Changed2.Advise(lifetime, ProcessChange);
    }

    private void AddReferencingProjects(IProject project, HashSet<IProject> projects)
    {
      if (projects.Contains(project)) return;
      projects.Add(project);
      foreach (var referencingProject in project.GetReferencingProjects(project.GetCurrentTargetFrameworkId()))
        if (referencingProject.ProjectProperties is FSharpProjectProperties)
          AddReferencingProjects(referencingProject, projects);
    }

    private void DoUpdateProjects()
    {
      // todo: reparse files, reprocess caches

      mySolution.Locks.AssertMainThread();

      var referencingProjects = new HashSet<IProject>();
      foreach (var project in myProjectsToUpdate)
        AddReferencingProjects(project, referencingProjects);
      myProjectsToUpdate.addAll(referencingProjects);

      var projectsOptions = new Dictionary<IProject, FSharpProjectOptions>();
      foreach (var project in myProjectsToUpdate)
        projectsOptions.Add(project, myProjectOptionsBuilder.BuildWithoutReferencedProjects(project));

      foreach (var projectOptions in myProjectOptionsBuilder.AddReferencedProjects(projectsOptions))
      {
        var project = projectOptions.Key;
        var options = projectOptions.Value;
        myFSharpCheckerService.AddProject(project, options, myProjectOptionsBuilder.GetDefines(project));
      }
      myProjectsToUpdate.Clear();
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
            myFSharpCheckerService.RemoveProject(project);
            return;
          }
          myProjectsToUpdate.Add(project);
          myUpdateEvent.FireIncoming();
        }
        return;
      }
      foreach (var projectModelChange in change.GetChildren())
        ProcessChange(projectModelChange);
    }
  }
}