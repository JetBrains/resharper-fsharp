using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.DataFlow;
using JetBrains.Platform.ProjectModel.FSharp;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.Impl;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.Util;
using Microsoft.FSharp.Collections;
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
    private readonly Lifetime myLifetime;
    private readonly FSharpChecker myChecker;

    private readonly bool myIsRunningOnMono; // this should be removed when Rider's bundled mono is able to call msbuild
    private readonly string myFSharpCorePath;

    private readonly Dictionary<Guid, FSharpProjectOptions> myProjectsOptions =
      new Dictionary<Guid, FSharpProjectOptions>();

    private readonly Dictionary<Guid, string[]> myProjectDefines =
      new Dictionary<Guid, string[]>();

    private readonly Dictionary<string, int> myFilesVersions =
      new Dictionary<string, int>(); // todo: clean it up when removing project containing file

    private readonly Dictionary<string, FSharpProjectOptions> myScriptOptions =
      new Dictionary<string, FSharpProjectOptions>();

    private CancellationTokenSource myCancellationTokenSource;
    private readonly ITaskHost myTaskHost;

    public FSharpCheckerService(Lifetime lifetime, OnSolutionCloseNotifier onSolutionCloseNotifier,
      ShellLocks shellLocks)
    {
      myLifetime = lifetime;
      myTaskHost = shellLocks.Tasks;

      myIsRunningOnMono = PlatformUtil.IsRunningOnMono;

      myChecker = FSharpChecker.Create(
        projectCacheSize: null, // use default value
        keepAssemblyContents: FSharpOption<bool>.Some(true),
        keepAllBackgroundResolutions: null, // use default value, true
        msbuildEnabled: null); // use default value, true

      onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(lifetime, () =>
      {
        myProjectsOptions.Clear();
        myProjectDefines.Clear();
        myChecker.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients();
      });

      if (!myIsRunningOnMono)
        return;

      var fsharpCorePath = Assembly.GetAssembly(typeof(Unit))?.Location;
      if (fsharpCorePath != null)
        myFSharpCorePath = "-r:" + FileSystemPath.Parse(fsharpCorePath);
    }

    [CanBeNull]
    public FSharpCheckFileResults GetPreviousCheckResults([NotNull] IPsiSourceFile sourceFile)
    {
      var filePath = sourceFile.GetLocation().FullPath;
      var projectOptions =
        Equals(sourceFile.LanguageType, FSharpScriptProjectFileType.Instance)
          ? myScriptOptions.GetValueSafe(filePath)
          : GetProjectOptions(sourceFile);

      return projectOptions != null
        ? myChecker.TryGetRecentCheckResultsForFile(filePath, projectOptions, null)?.Value.Item2
        : null;
    }

    [CanBeNull]
    public FSharpOption<FSharpParseFileResults> ParseFSharpFile([NotNull] IPsiSourceFile sourceFile)
    {
      var projectOptions = GetProjectOptions(sourceFile, true);
      if (projectOptions == null)
        return null;

      var filename = sourceFile.GetLocation().FullPath;
      var source = sourceFile.Document.GetText();
      return OptionModule.OfObj(myChecker.ParseFileInProject(filename, source, projectOptions).RunAsTask());
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
      if (projectOptions == null)
        return null;

      var filename = sourceFile.GetLocation().FullPath;
      var source = sourceFile.Document.GetText();
      var version = myFilesVersions[filename] = myFilesVersions.GetOrCreateValue(filename, -1) + 1;

      var checkAsync = myChecker.CheckFileInProject(fsFile.ParseResults, filename, version, source, projectOptions,
        textSnapshotInfo: null);
      return (checkAsync.RunAsTask(interruptChecker) as FSharpCheckFileAnswer.Succeeded)?.Item;
    }

    [CanBeNull]
    private FSharpProjectOptions GetProjectOptions([NotNull] IPsiSourceFile sourceFile, bool tryUpdate = false)
    {
      var projectFile = sourceFile.ToProjectFile();
      if (projectFile != null && !projectFile.Properties.BuildAction.IsCompile() &&
          !Equals(projectFile.LanguageType, FSharpScriptProjectFileType.Instance))
        return null;

      if (sourceFile.LanguageType.Equals(FSharpScriptProjectFileType.Instance))
        return GetScriptOptions(sourceFile, tryUpdate);
      var project = sourceFile.GetProject();
      return (project != null ? myProjectsOptions.GetValueSafe(project.Guid) : null) ??
             GetScriptOptions(sourceFile, tryUpdate);
    }

    [CanBeNull]
    private FSharpProjectOptions GetScriptOptions([NotNull] IPsiSourceFile sourceFile, bool shouldTryUpdate = false)
    {
      var filePath = sourceFile.GetLocation().FullPath;
      var source = sourceFile.Document.GetText();
      var loadTime = FSharpOption<DateTime>.Some(DateTime.Now);
      var getScriptOptionsAsync =
        myChecker.GetProjectOptionsFromScript(filePath, source, loadTime, otherFlags: null, useFsiAuxLib: null,
          assumeDotNetFramework: null, extraProjectInfo: null);

      if (!myScriptOptions.ContainsKey(filePath))
      {
        try
        {
          var scriptOptionsAndErrors = getScriptOptionsAsync.RunAsTask();
          var options = FixOptions(scriptOptionsAndErrors?.Item1);
          if (options == null) return null;

          myScriptOptions[filePath] = options;
          return options;
        }
        catch (Exception)
        {
          // Error while resolving assemblies
          // todo: replace FCS reference resolver
          return null;
        }
      }

      var existingOptions = myScriptOptions[filePath];
      if (!shouldTryUpdate)
        return existingOptions;

      myCancellationTokenSource?.Cancel();
      myCancellationTokenSource = new CancellationTokenSource();
      myTaskHost.StartNew(myLifetime, Scheduling.FreeThreaded, () =>
      {
        try
        {
          var cancellationToken = FSharpOption<CancellationToken>.Some(myCancellationTokenSource.Token);
          var optionsAndErrors = FSharpAsync.RunSynchronously(getScriptOptionsAsync, null, cancellationToken);
          var options = FixOptions(optionsAndErrors?.Item1);
          if (options != null && !ArrayUtil.Equals(options.OtherOptions, existingOptions.OtherOptions))
          {
            myScriptOptions[filePath] = options;
            sourceFile.GetSolution().GetComponent<DaemonImpl>().Invalidate();
          }
          myCancellationTokenSource = null;
        }
        catch (Exception)
        {
          // Error while resolving assemblies
          // todo: replace FCS reference resolver
        }
      });
      return existingOptions;
    }

    [CanBeNull]
    private FSharpProjectOptions FixOptions([CanBeNull] FSharpProjectOptions options)
    {
      if (options == null || !myIsRunningOnMono || myFSharpCorePath == null)
        return options;

      return new FSharpProjectOptions(
        options.ProjectFileName,
        options.ProjectFileNames,
        ArrayModule.Append(options.OtherOptions, new[] {myFSharpCorePath}),
        options.ReferencedProjects,
        options.IsIncompleteTypeCheckEnvironment,
        options.UseScriptResolutionRules,
        options.LoadTime,
        options.UnresolvedReferences,
        options.OriginalLoadReferences,
        options.ExtraProjectInfo);
    }

    [NotNull]
    public string[] GetDefinedConstants(IPsiSourceFile sourceFile)
    {
      var project = sourceFile.GetProject();
      Assertion.AssertNotNull(project, "project != null");
      return myProjectDefines.GetValueSafe(project.Guid) ?? EmptyArray<string>.Instance;
    }

    /// <summary>
    /// Remember project options and start analyzing the project in background.
    /// If the project is already known and options have changed, invalidate and start over.
    /// </summary>
    public void AddProject(Guid projectGuid, [NotNull] FSharpProjectOptions projectOptions,
      [NotNull] string[] defines)
    {
      if (myProjectsOptions.ContainsKey(projectGuid))
      {
        if (myProjectsOptions[projectGuid].Equals(projectOptions)) return;
        myChecker.InvalidateConfiguration(projectOptions);
      }

      myProjectsOptions[projectGuid] = projectOptions;
      myProjectDefines[projectGuid] = defines;
      myChecker.CheckProjectInBackground(projectOptions);
    }

    /// <summary>
    /// Invalidate project and stop background analysis.
    /// </summary>
    public void RemoveProject(Guid projectGuid)
    {
      Assertion.Assert(myProjectsOptions.ContainsKey(projectGuid), "myProjectsOptions.ContainsKey(project)");
      myChecker.InvalidateConfiguration(myProjectsOptions[projectGuid]);
      myProjectsOptions.Remove(projectGuid);
      myProjectDefines.Remove(projectGuid);
    }
  }
}