using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.Platform.ProjectModel.FSharp;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Psi.FSharp
{
  /// <summary>
  /// Wrapper for FSharp.Compiler.Service FSharpChecker.
  /// When F# project is added to solution, FSharpChecker starts analyzing it in background.
  /// </summary>
  [ShellComponent]
  public class FSharpCheckerService
  {
    private readonly FSharpChecker myChecker;

    private readonly Dictionary<IProject, FSharpProjectOptions> myProjectsOptions =
      new Dictionary<IProject, FSharpProjectOptions>();

    private readonly Dictionary<IProject, string[]> myProjectDefines =
      new Dictionary<IProject, string[]>();

    private readonly Dictionary<string, int> myFilesVersions =
      new Dictionary<string, int>(); // todo: clean it up when removing project containing file

    public FSharpCheckerService(Lifetime lifetime, OnSolutionCloseNotifier onSolutionCloseNotifier)
    {
      myChecker = FSharpChecker.Create(
        projectCacheSize: null, // use default value
        keepAssemblyContents: FSharpOption<bool>.Some(true),
        keepAllBackgroundResolutions: null, // use default value, true
        msbuildEnabled: new FSharpOption<bool>(false));

      onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(lifetime, () =>
      {
        myProjectsOptions.Clear();
        myProjectDefines.Clear();
        myChecker.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients();
      });
    }

    [CanBeNull]
    public FSharpCheckFileResults GetPreviousCheckResults([NotNull] IPsiSourceFile sourceFile)
    {
      var projectOptions = GetProjectOptions(sourceFile);
      var filename = sourceFile.GetLocation().FullPath;
      return myChecker.TryGetRecentCheckResultsForFile(filename, projectOptions, null)?.Value.Item2;
    }

    [CanBeNull]
    public FSharpParseFileResults ParseFSharpFile([NotNull] IPsiSourceFile sourceFile)
    {
      var projectOptions = GetProjectOptions(sourceFile);
      var filename = sourceFile.GetLocation().FullPath;
      var source = sourceFile.Document.GetText();
      return myChecker.ParseFileInProject(filename, source, projectOptions).RunAsTask();
    }

    /// <param name="fsFile"></param>
    /// <param name="interruptChecker">Optional, if not provided InterruptableActivityCookie is used.</param>
    /// <returns></returns>
    [CanBeNull]
    public FSharpCheckFileResults CheckFSharpFile([NotNull] IFSharpFileCheckInfoOwner fsFile,
      [CanBeNull] Action interruptChecker = null)
    {
      var sourceFile = fsFile.GetSourceFile();
      if (sourceFile == null)
        return null;

      var projectOptions = GetProjectOptions(sourceFile);
      var filename = sourceFile.GetLocation().FullPath;
      var source = sourceFile.Document.GetText();
      var version = myFilesVersions[filename] = myFilesVersions.GetOrCreateValue(filename, -1) + 1;

      var checkAsync = myChecker.CheckFileInProject(fsFile.ParseResults, filename, version, source, projectOptions,
        textSnapshotInfo: null);
      return (checkAsync.RunAsTask(interruptChecker) as FSharpCheckFileAnswer.Succeeded)?.Item;
    }

    [NotNull]
    private FSharpProjectOptions GetProjectOptions([NotNull] IPsiSourceFile sourceFile)
    {
      if (sourceFile.LanguageType.Equals(FSharpScriptProjectFileType.Instance))
        return GetScriptOptions(sourceFile);
      var project = sourceFile.GetProject();
      return (project != null ? myProjectsOptions.GetValueSafe(project) : null) ?? GetScriptOptions(sourceFile);
    }

    [NotNull]
    private FSharpProjectOptions GetScriptOptions([NotNull] IPsiSourceFile sourceFile)
    {
      var filePath = sourceFile.GetLocation().FullPath;
      var source = sourceFile.Document.GetText();
      var loadTime = FSharpOption<DateTime>.Some(DateTime.Now);

      var getScriptOptionsAsync =
        myChecker.GetProjectOptionsFromScript(filePath, source, loadTime, otherFlags: null, useFsiAuxLib: null,
          assumeDotNetFramework: null, extraProjectInfo: null);
      return FSharpAsync.RunSynchronously(getScriptOptionsAsync, null, null);
    }

    [NotNull]
    public string[] GetDefinedConstants(IPsiSourceFile sourceFile)
    {
      var project = sourceFile.GetProject();
      Assertion.AssertNotNull(project, "project != null");
      return myProjectDefines.GetValueSafe(project) ?? EmptyArray<string>.Instance;
    }

    /// <summary>
    /// Remember project options and start analyzing the project in background.
    /// If the project is already known and options have changed, invalidate and start over.
    /// </summary>
    public void AddProject([NotNull] IProject project, [NotNull] FSharpProjectOptions projectOptions,
      [NotNull] string[] defines)
    {
      if (myProjectsOptions.ContainsKey(project))
      {
        if (myProjectsOptions[project].Equals(projectOptions)) return;
        myChecker.InvalidateConfiguration(projectOptions);
      }

      myProjectsOptions[project] = projectOptions;
      myProjectDefines[project] = defines;
      myChecker.CheckProjectInBackground(projectOptions);
    }

    /// <summary>
    /// Invalidate project and stop background analysis.
    /// </summary>
    public void RemoveProject([NotNull] IProject project)
    {
      Assertion.Assert(myProjectsOptions.ContainsKey(project), "myProjectsOptions.ContainsKey(project)");
      myChecker.InvalidateConfiguration(myProjectsOptions[project]);
      myProjectsOptions.Remove(project);
      myProjectDefines.Remove(project);
    }
  }
}