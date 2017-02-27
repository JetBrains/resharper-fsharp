using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Platform.MsBuildModel;
using JetBrains.Platform.ProjectModel.FSharp.Properties;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Model2.Assemblies.Interfaces;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.MsBuild;
using JetBrains.ProjectModel.ProjectsHost.SolutionHost;
using JetBrains.ProjectModel.Properties.Managed;
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

    public FSharpProjectOptionsBuilder(ISolution solution)
    {
      myMsBuildHost = solution.ProjectsHostContainer().GetComponent<MsBuildProjectHost>();
    }

    /// <summary>
    /// Build project options for FSharp.Compiler.Service. Options for referenced projects are not created.
    /// Use AddReferencedProjects to add referenced projects options.
    /// </summary>
    public FSharpProjectOptions BuildWithoutReferencedProjects(IProject project)
    {
      using (ReadLockCookie.Create())
      {
        var buildSettings = project.ProjectProperties.BuildSettings as IManagedProjectBuildSettings;
        var projectOptions = new List<string>
        {
          "--out:" + project.GetOutputFilePath(project.GetCurrentTargetFrameworkId()),
          "--simpleresolution",
          "--noframework",
          "--debug:full",
          "--debug+",
          "--optimize-",
          "--tailcalls-",
          "--fullpaths",
          "--flaterrors",
          "--highentropyva+",
          "--target:" + GetOutputTarget(buildSettings),
          "--platform:anycpu" // todo
//          "--warnon:", // todo
//          xml doc // todo
        };

        projectOptions.addAll(GetDefines(project).Convert(s => "--define:" + s));
        projectOptions.addAll(GetReferencedPathsOptions(project));

        var projectFileNames = new List<string>();
        var projectMark = project.GetProjectMark().NotNull();
        myMsBuildHost.mySessionHolder.Execute(session =>
        {
          session.EditProject(projectMark, editor =>
          {
            // obtains all items in a right order directly from msbuild
            foreach (var item in editor.Items)
            {
              if (!BuildAction.GetOrCreate(item.ItemType()).IsCompile()) continue;

              var path = FileSystemPath.TryParse(item.EvaluatedInclude);
              if (path.IsEmpty) continue;

              var actualPath = EnsureAbsolute(path, projectMark.Location.Directory);
              projectFileNames.Add(actualPath.FullPath);
            }
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

    private string GetOutputTarget([CanBeNull] IManagedProjectBuildSettings buildSettings)
    {
      if (buildSettings == null) return "library";

      switch (buildSettings.OutputType)
      {
        case ProjectOutputType.CONSOLE_EXE:
          return "exe";
        case ProjectOutputType.LIBRARY:
          return "library";
        default:
          return "library"; // todo: any additional cases for F#?
      }
    }

    [NotNull]
    private FileSystemPath EnsureAbsolute([NotNull] FileSystemPath path, [NotNull] FileSystemPath projectDirectory)
    {
      var relativePath = path.AsRelative();
      if (relativePath == null) return path;
      return projectDirectory.Combine(relativePath);
    }

    /// <summary>
    /// Current configuration defines, e.g. TRACE or DEBUG. Used in FSharpLexer.
    /// </summary>
    [NotNull]
    public string[] GetDefines([NotNull] IProject project)
    {
      // todo: multiple frameworks?
      var activeConfigurations = project.ProjectProperties.ActiveConfigurations;
      var configuration = activeConfigurations.Configurations.SingleItem() as IManagedProjectConfiguration;
      var definesString = configuration?.DefineConstants;

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
      [NotNull] IDictionary<IProject, FSharpProjectOptions> options)
    {
      using (ReadLockCookie.Create())
      {
        var newOptions = new Dictionary<IProject, FSharpProjectOptions>();
        foreach (var project in options.Keys)
          if (!newOptions.ContainsKey(project))
            newOptions[project] = AddReferencedProjects(project, options, newOptions);
        return newOptions;
      }
    }

    [NotNull]
    private static FSharpProjectOptions AddReferencedProjects([NotNull] IProject project,
      [NotNull] IDictionary<IProject, FSharpProjectOptions> options,
      [NotNull] IDictionary<IProject, FSharpProjectOptions> newOptions)
    {
      // do not transitively add referenced projects (same as in other FSharp tools)
      var referencedProjects = project.GetReferencedProjects(project.GetCurrentTargetFrameworkId(), transitive: false);
      var referencedProjectsOptions = new List<Tuple<string, FSharpProjectOptions>>();
      foreach (var referencedProject in referencedProjects)
      {
        if (!(referencedProject.ProjectProperties is FSharpProjectProperties) ||
            newOptions.ContainsKey(project)) continue;

        var fixedOptions = AddReferencedProjects(referencedProject, options, newOptions);
        newOptions[referencedProject] = fixedOptions;
        var outPath = referencedProject.GetOutputFilePath(referencedProject.GetCurrentTargetFrameworkId()).FullPath;
        referencedProjectsOptions.Add(Tuple.Create(outPath, fixedOptions));
      }
      var oldOptions = options[project];
      return new FSharpProjectOptions(
        oldOptions.ProjectFileName,
        oldOptions.ProjectFileNames,
        oldOptions.OtherOptions,
        referencedProjectsOptions.ToArray(),
        oldOptions.IsIncompleteTypeCheckEnvironment,
        oldOptions.UseScriptResolutionRules,
        oldOptions.LoadTime,
        oldOptions.UnresolvedReferences);
    }
  }
}