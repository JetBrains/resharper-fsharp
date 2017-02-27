using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.DataFlow;
using JetBrains.Platform.MsBuildModel;
using JetBrains.Platform.ProjectModel.FSharp.Properties;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl.Build;
using JetBrains.ProjectModel.Model2.Assemblies.Interfaces;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.MsBuild;
using JetBrains.ProjectModel.ProjectsHost.SolutionHost;
using JetBrains.ReSharper.PsiGen.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp
{
  [SolutionComponent]
  public class FSharpProjectOptionsBuilder
  {
    private readonly MsBuildProjectHost myMsBuildHost;

    public FSharpProjectOptionsBuilder(Lifetime lifetime, ISolution solution)
    {
      myMsBuildHost = solution.ProjectsHostContainer().GetComponent<MsBuildProjectHost>();
    }

    /// <summary>
    /// Build project options for FSharp.Compiler.Service. Options for referenced projects are not created.
    /// </summary>
    public FSharpProjectOptions BuildWithoutReferencedProjects(IProject project)
    {
      using (ReadLockCookie.Create())
      {
        var targetFrameworkId = project.GetCurrentTargetFrameworkId();
        var projectOptions = new List<string>
        {
          "--out:" + project.GetOutputFilePath(targetFrameworkId),
          "--simpleresolution",
          "--noframework",
          "--debug:full",
          "--debug+",
          "--optimize-",
          "--tailcalls-",
          "--fullpaths",
          "--flaterrors",
          "--highentropyva+",
          "--target:library", // todo
//            "--subsystemversion:6.00",
//            "--warnon:",
          "--platform:anycpu"
        };

        // todo: Get all properties for compiler from proper IProject.Configurations
        projectOptions.addAll(GetDefines(project).Convert(s => "--define:" + s));
        projectOptions.addAll(GetReferencedPathsOptions(project));

        var projectFileNames = new List<string>();
        var projectMark = project.GetProjectMark().NotNull();
        myMsBuildHost.mySessionHolder.Execute(session =>
        {
          session.EditProject(projectMark, editor =>
          {
            // Obtains all items in a right order directly from msbuild
            foreach (var item in editor.Items)
              if (BuildAction.GetOrCreate(item.ItemType()).IsCompile())
                projectFileNames.Add(item.EvaluatedInclude); // todo: need full paths here
          });
        });

        return new FSharpProjectOptions(
          project.ProjectFileLocation.FullPath,
          projectFileNames.ToArray(),
          projectOptions.ToArray(),
          referencedProjects: EmptyArray<Tuple<string, FSharpProjectOptions>>.Instance,
          isIncompleteTypeCheckEnvironment: false,
          useScriptResolutionRules: false,
          loadTime: DateTime.Now,
          unresolvedReferences: null
        );
      }
    }

    /// <summary>
    /// Current configuration defines, e.g. TRACE or DEBUG. Used in FSharpLexer.
    /// </summary>
    [NotNull]
    public string[] GetDefines([NotNull] IProject project)
    {
      var configurations = (project as ProjectImpl)?.ProjectProperties.ActiveConfigurations.Configurations;
      var definesString = configurations?.OfType<ManagedProjectConfiguration>().FirstOrDefault()?.DefineConstants;
      return string.IsNullOrEmpty(definesString)
        ? EmptyArray<string>.Instance
        : definesString.Split(';', ',', ' ').Select(x => x.Trim()).ToArray();
    }

    [NotNull]
    private IEnumerable<string> GetReferencedPathsOptions(IProject project)
    {
      // todo: provide path to dll for unloaded projects?
      var framework = project.GetCurrentTargetFrameworkId();
      var refProjectsOutputs = project.GetReferencedProjects(framework)
        .Select(p => p.GetOutputFilePath(p.GetCurrentTargetFrameworkId()));
      var refAssembliesPaths = project.GetAssemblyReferences(framework)
        .Select(a => a.ResolveResultAssemblyFile().Location);
      return refProjectsOutputs.Concat(refAssembliesPaths).Select(a => "-r:" + a.FullPath);
    }

    [NotNull]
    public Dictionary<IProject, FSharpProjectOptions> AddReferencedProjects(
      [NotNull] Dictionary<IProject, FSharpProjectOptions> optionsWithoutRerefencedProjects)
    {
      using (ReadLockCookie.Create())
      {
        var newOptions = new Dictionary<IProject, FSharpProjectOptions>();
        foreach (var projectOptions in optionsWithoutRerefencedProjects)
        {
          var project = projectOptions.Key;
          newOptions[project] = AddReferencedProjects(project, optionsWithoutRerefencedProjects, newOptions);
        }
        return newOptions;
      }
    }

    [NotNull]
    private FSharpProjectOptions AddReferencedProjects([NotNull] IProject project,
      [NotNull] Dictionary<IProject, FSharpProjectOptions> optionsWithoutReferences,
      [NotNull] Dictionary<IProject, FSharpProjectOptions> newOptions)
    {
      if (newOptions.ContainsKey(project)) return newOptions[project];

      // do not transitively add referenced projects like other FSharp tools (maybe change it later)
      var referencedProjects = project.GetReferencedProjects(project.GetCurrentTargetFrameworkId(), transitive: false);
      var referencedProjectOptions = new List<Tuple<string, FSharpProjectOptions>>();
      foreach (var referencedProject in referencedProjects)
      {
        if (!(referencedProject.ProjectProperties is FSharpProjectProperties)) continue;

        var fixedOptions = AddReferencedProjects(referencedProject, optionsWithoutReferences, newOptions);
        newOptions[referencedProject] = fixedOptions;
        var outPath = referencedProject.GetOutputFilePath(referencedProject.GetCurrentTargetFrameworkId()).FullPath;
        referencedProjectOptions.Add(Tuple.Create(outPath, fixedOptions));
      }
      var oldOptions = optionsWithoutReferences[project];
      return new FSharpProjectOptions(
        oldOptions.ProjectFileName,
        oldOptions.ProjectFileNames,
        oldOptions.OtherOptions,
        referencedProjectOptions.ToArray(),
        oldOptions.IsIncompleteTypeCheckEnvironment,
        oldOptions.UseScriptResolutionRules,
        oldOptions.LoadTime,
        oldOptions.UnresolvedReferences);
    }
  }
}