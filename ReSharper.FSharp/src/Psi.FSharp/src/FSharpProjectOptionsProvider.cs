using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.DataFlow;
using JetBrains.Platform.ProjectModel.FSharp.Properties;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl.Build;
using JetBrains.ProjectModel.Model2.Assemblies.Interfaces;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Psi.FSharp
{
  [SolutionComponent]
  public class FSharpProjectOptionsProvider : IChangeProvider
  {
    private readonly ISolution mySolution;
    private readonly PsiModules myPsiModules;
    private readonly OnSolutionCloseNotifier myNotifier;

    private static readonly Dictionary<IProject, FSharpProjectOptions> ourProjectsOptions =
      new Dictionary<IProject, FSharpProjectOptions>();

    private static readonly Dictionary<IProject, string[]> ourConfigurationDefines =
      new Dictionary<IProject, string[]>();

    private static readonly FSharpFunc<string, bool> ourOptionsFilter =
      FSharpFunc<string, bool>.FromConverter(s => !s.StartsWith("--out:", StringComparison.Ordinal) &&
                                                  !s.StartsWith("-r:", StringComparison.Ordinal));

    private const string Configuration = "Configuration";

    public FSharpProjectOptionsProvider(Lifetime lifetime, ISolution solution, PsiModules psiModules,
      OnSolutionCloseNotifier notifier, ChangeManager changeManager)
    {
      mySolution = solution;
      myPsiModules = psiModules;
      myNotifier = notifier;
      changeManager.RegisterChangeProvider(lifetime, this);
      changeManager.AddDependency(lifetime, this, myPsiModules);
      changeManager.AddDependency(lifetime, this, mySolution);

      myNotifier.SolutionIsAboutToClose.Advise(lifetime, () =>
      {
        ourProjectsOptions.Clear();
        FSharpCheckerUtil.Checker.InvalidateAll();
      });
    }

    public object Execute(IChangeMap changeMap)
    {
      var moduleChanges = changeMap.GetChange<PsiModuleChange>(myPsiModules)?.ModuleChanges;
      if (moduleChanges == null) return null;

      foreach (var moduleChange in moduleChanges)
      {
        var changeType = moduleChange.Type;
        if (changeType != PsiModuleChange.ChangeType.Added && changeType != PsiModuleChange.ChangeType.Removed)
          continue;

        var project = moduleChange.Item.ContainingProjectModule as IProject;
        if (project == null || !project.IsValid() || !(project.ProjectProperties is FSharpProjectProperties))
          continue;

        if (changeType == PsiModuleChange.ChangeType.Added)
        {
          var newProjectOptions = GetProjectOptions(project);
          if (newProjectOptions == null) continue;
          ourProjectsOptions[project] = newProjectOptions;
          ourConfigurationDefines[project] = GetDefines(project);
          FSharpCheckerUtil.Checker.CheckProjectInBackground(newProjectOptions);
        }
        if (changeType == PsiModuleChange.ChangeType.Removed)
        {
          FSharpCheckerUtil.Checker.InvalidateConfiguration(ourProjectsOptions[project]);
          ourProjectsOptions.Remove(project);
          ourConfigurationDefines.Remove(project);
        }
      }
      return null;
    }

    [CanBeNull]
    public static FSharpProjectOptions GetProjectOptions([NotNull] IPsiSourceFile sourceFile)
    {
      if (sourceFile.LanguageType.Equals(FSharpScriptProjectFileType.Instance)) return GetScriptOptions(sourceFile);
      var project = sourceFile.GetProject();
      var options = project != null ? ourProjectsOptions.GetValueSafe(project) : null;
      return options ?? GetScriptOptions(sourceFile);
    }

    [NotNull]
    public static FSharpProjectOptions GetScriptOptions([NotNull] IPsiSourceFile sourceFile)
    {
      var filePath = sourceFile.GetLocation().FullPath;
      var getScriptOptionsAsync = FSharpCheckerUtil.Checker.GetProjectOptionsFromScript(
        filePath, sourceFile.Document.GetText(), FSharpOption<DateTime>.Some(DateTime.Now), null, null);
      return FSharpAsync.RunSynchronously(getScriptOptionsAsync, null, null);
    }

    [CanBeNull]
    public static FSharpProjectOptions GetProjectOptions([NotNull] IProject project)
    {
      var path = project.ProjectFile?.Location.FullPath;
      var configuration = project.ProjectProperties.ActiveConfigurations.Configurations.FirstOrDefault();
      if (path == null || configuration == null) return null;
      var configurationName = ListModule.OfArray(new[] {Tuple.Create(Configuration, configuration.Name)});

      var optionsFromCracker = ProjectCracker.GetProjectOptionsFromProjectFile(path,
        OptionModule.OfObj(configurationName), FSharpOption<DateTime>.Some(DateTime.Now));
      var fixedOptions = FixReferences(optionsFromCracker, project).Item2;
      ourProjectsOptions[project] = fixedOptions;
      return fixedOptions;
    }

    [NotNull]
    private static IProject GetProject([NotNull] FSharpProjectOptions options, [NotNull] ISolution solution)
    {
      var project = solution.FindProjectByProjectFilePath(FileSystemPath.Parse(options.ProjectFileName));
      Assertion.AssertNotNull(project, "project != null");
      return project;
    }

    [NotNull]
    private static Tuple<string, FSharpProjectOptions> FixReferences([NotNull] FSharpProjectOptions options,
      [NotNull] IProject project)
    {
      var solution = project.GetSolution();
      var outPath = project.GetOutputFilePath(project.GetCurrentTargetFrameworkId()).FullPath;
      var filteredOptions = ArrayModule.Filter(ourOptionsFilter, options.OtherOptions);
      var referencedPathsOptions = GetReferencedPathsOptions(project);
      var definesOptions = GetDefines(project).Convert(s => "--define:" + s);

      var fixedOptions = new FSharpProjectOptions(
        options.ProjectFileName,
        options.ProjectFileNames,
        ArrayModule.Concat(new[] {new[] {"--out:" + outPath}, filteredOptions, referencedPathsOptions, definesOptions}),
        options.ReferencedProjects.Convert(p => FixReferences(p.Item2, GetProject(p.Item2, solution))),
        options.IsIncompleteTypeCheckEnvironment,
        options.UseScriptResolutionRules,
        options.LoadTime,
        options.UnresolvedReferences);

      return Tuple.Create(outPath, fixedOptions);
    }

    [NotNull]
    private static string[] GetReferencedPathsOptions(IProject project)
    {
      var framework = project.GetCurrentTargetFrameworkId();
      var refProjectsOutputs = project.GetReferencedProjects(framework)
        .Select(p => p.GetOutputFilePath(p.GetCurrentTargetFrameworkId()));
      var refAssembliesPaths = project.GetAssemblyReferences(framework)
        .Select(a => a.ResolveResultAssemblyFile().Location);
      return refProjectsOutputs.Concat(refAssembliesPaths).Select(a => "-r:" + a.FullPath).ToArray();
    }

    [NotNull]
    public static string[] GetDefinedConstants([NotNull] IPsiSourceFile sourceFile)
    {
      var project = sourceFile.GetProject();
      return project != null && ourConfigurationDefines.ContainsKey(project)
        ? ourConfigurationDefines[project]
        : EmptyArray<string>.Instance;
    }

    [NotNull]
    public static string[] SplitDefines([CanBeNull] string definesString)
    {
      if (string.IsNullOrEmpty(definesString)) return EmptyArray<string>.Instance;

      var defines = definesString.Split(';', ',', ' ');
      var result = new string[defines.Length];
      for (var i = 0; i < defines.Length; i++)
        result[i] = defines[i].Trim();
      return result;
    }

    [NotNull]
    public static string[] GetDefines([NotNull] IProject project)
    {
      var configurations = (project as ProjectImpl)?.ProjectProperties.ActiveConfigurations.Configurations;
      var definesString = configurations?.OfType<ManagedProjectConfiguration>().FirstOrDefault()?.DefineConstants;
      return SplitDefines(definesString);
    }
  }
}