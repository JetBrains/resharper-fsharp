using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Platform.MsBuildModel;
using JetBrains.Platform.ProjectModel.FSharp.ProjectProperties;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Model2.Assemblies.Interfaces;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.MsBuild;
using JetBrains.ProjectModel.ProjectsHost.SolutionHost;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.ProjectOptions
{
  [Obsolete]
  public class FSharpProjectOptionsBuilderOld
  {
    private readonly MsBuildProjectHost myMsBuildHost;

    public FSharpProjectOptionsBuilderOld(ISolution solution)
    {
      myMsBuildHost = solution.ProjectsHostContainer().GetComponent<MsBuildProjectHost>();
    }

    /// <summary>
    /// Build project options for FSharp.Compiler.Service. Options for referenced projects are not created.
    /// Use AddReferencedProjects to add referenced projects options.
    /// </summary>
    public FSharpProjectOptions BuildWithoutReferencedProjects(IProject project)
    {
      Assertion.AssertNotNull(project, "project != null");
      var buildSettings = project.ProjectProperties.BuildSettings as IManagedProjectBuildSettings;
      var configuration =
        project.ProjectProperties.ActiveConfigurations.Configurations.SingleItem() as IManagedProjectConfiguration;
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
        "--platform:anycpu", // todo
      };

      if (configuration != null)
      {
        projectOptions.AddRange(SplitDefines(configuration.DefineConstants).Convert(s => "--define:" + s));

        var doc = configuration.DocumentationFile;
        if (!doc.IsNullOrWhitespace()) projectOptions.Add("--doc:" + doc);

        var nowarn = FixNoWarn(configuration.NoWarn);
        if (!nowarn.IsNullOrWhitespace()) projectOptions.Add("--nowarn:" + nowarn);

        projectOptions.Add("--warn:" + configuration.WarningLevel);

        // todo: add F# specific options like 'warnon'
      }

      projectOptions.AddRange(GetReferencedPathsOptions(project));

      var projectFileNames = new List<string>();
      var projectMark = project.GetProjectMark().NotNull();
      myMsBuildHost.mySessionHolder.Execute(session =>
      {
        session.EditProject(projectMark, editor =>
        {
          // obtains all items in a right order directly from msbuild
          foreach (var item in editor.Items)
            if (BuildAction.GetOrCreate(item.ItemType()).IsCompile())
            {
              var path = FileSystemPath.TryParse(item.EvaluatedInclude);
              if (!path.IsEmpty)
                projectFileNames.Add(EnsureAbsolute(path, projectMark.Location.Directory).FullPath);
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
        unresolvedReferences: null,
        originalLoadReferences: FSharpList<Tuple<Range.range, string>>.Empty,
        extraProjectInfo: null
      );
    }

    private static string GetOutputTarget([CanBeNull] IManagedProjectBuildSettings buildSettings)
    {
      switch (buildSettings?.OutputType)
      {
        case ProjectOutputType.CONSOLE_EXE:
          return "exe";
        case ProjectOutputType.WIN_EXE:
          return "winexe";
        case ProjectOutputType.MODULE:
          return "module";
        default:
          return "library";
      }
    }

    [NotNull]
    private static FileSystemPath EnsureAbsolute([NotNull] FileSystemPath path,
      [NotNull] FileSystemPath projectDirectory)
    {
      var relativePath = path.AsRelative();
      return relativePath != null
        ? projectDirectory.Combine(relativePath)
        : path;
    }

    [NotNull]
    private static string FixNoWarn([CanBeNull] string noWarn)
    {
      return SplitAndTrim(noWarn).Join(",");
    }

    [NotNull]
    private static string[] SplitDefines([CanBeNull] string definesString)
    {
      return SplitAndTrim(definesString).ToArray();
    }

    [NotNull]
    private static IEnumerable<string> SplitAndTrim([CanBeNull] string strings)
    {
      return strings?.Split(';', ',', ' ').Select(x => x.Trim()).Where(s => !s.IsEmpty()) ??
             EmptyList<string>.Instance;
    }

    /// <summary>
    /// Current configuration defines, e.g. TRACE or DEBUG. Used in FSharpLexer.
    /// </summary>
    [NotNull]
    public string[] GetDefines(IProject project)
    {
      using (ReadLockCookie.Create())
      {
        Assertion.AssertNotNull(project, "project != null");
        var configuration =
          project.ProjectProperties.ActiveConfigurations.Configurations.SingleItem() as IManagedProjectConfiguration;
        return SplitDefines(configuration?.DefineConstants);
      }
    }

    [NotNull]
    private static IEnumerable<string> GetReferencedPathsOptions(IProject project)
    {
      var framework = project.GetCurrentTargetFrameworkId();
      var paths = new FrugalLocalList<string>();
      foreach (var referencedProject in project.GetReferencedProjects(framework))
        paths.Add("-r:" + referencedProject.GetOutputFilePath(referencedProject.GetCurrentTargetFrameworkId()));

      foreach (var assemblyReference in project.GetAssemblyReferences(framework))
        paths.Add("-r:" + assemblyReference.ResolveResultAssemblyFile().Location.FullPath);

      return paths.ToList();
    }

    [NotNull]
    public Dictionary<IProject, FSharpProjectOptions> AddReferencedProjects(
      [NotNull] IDictionary<IProject, FSharpProjectOptions> optionsDict)
    {
      var newOptions = new Dictionary<IProject, FSharpProjectOptions>();
      foreach (var projectAndOptions in optionsDict)
      {
        var project = projectAndOptions.Key;
        var projectOptions = projectAndOptions.Value;
        if (!newOptions.ContainsKey(project))
          newOptions[project] = AddReferencedProjects(project, projectOptions, optionsDict, newOptions);
      }

      return newOptions;
    }

    [NotNull]
    private FSharpProjectOptions AddReferencedProjects([NotNull] IProject project, FSharpProjectOptions projectOptions,
      [NotNull] IDictionary<IProject, FSharpProjectOptions> optionsDict,
      [NotNull] IDictionary<IProject, FSharpProjectOptions> newOptions)
    {
      // do not transitively add referenced projects (same as in other FSharp tools)
      var referencedProjects = project.GetReferencedProjects(project.GetCurrentTargetFrameworkId(), transitive: false);
      var referencedProjectsOptions = new List<Tuple<string, FSharpProjectOptions>>();
      foreach (var referencedProject in referencedProjects)
      {
        if (!(referencedProject.ProjectProperties is FSharpProjectProperties) ||
            newOptions.ContainsKey(project)) continue;

        var referencedProjectOptions = optionsDict.ContainsKey(referencedProject)
          ? optionsDict[referencedProject]
          : BuildWithoutReferencedProjects(referencedProject);

        var fixedOptions = AddReferencedProjects(referencedProject, referencedProjectOptions, optionsDict, newOptions);
        newOptions[referencedProject] = fixedOptions;
        var outPath = referencedProject.GetOutputFilePath(referencedProject.GetCurrentTargetFrameworkId()).FullPath;
        referencedProjectsOptions.Add(Tuple.Create(outPath, fixedOptions));
      }

      return new FSharpProjectOptions(
        projectOptions.ProjectFileName,
        projectOptions.ProjectFileNames,
        projectOptions.OtherOptions,
        referencedProjectsOptions.ToArray(),
        projectOptions.IsIncompleteTypeCheckEnvironment,
        projectOptions.UseScriptResolutionRules,
        projectOptions.LoadTime,
        projectOptions.UnresolvedReferences,
        projectOptions.OriginalLoadReferences,
        projectOptions.ExtraProjectInfo);
    }
  }
}