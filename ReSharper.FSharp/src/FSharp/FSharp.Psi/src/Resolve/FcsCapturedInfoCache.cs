using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using JetBrains.Application.ContentModel;
using JetBrains.Application.Parts;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.DocumentManagers.impl;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files.SandboxFiles;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Concurrency.Threading;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  [SolutionComponent(InstantiationEx.LegacyDefault)]
  public class FcsCapturedInfoCache : IPsiSourceFileCache, IFcsCapturedInfoCache
  {
    private readonly object mySyncObj = new();
    private readonly IShellLocks myLocks;
    public IFcsProjectProvider FcsProjectProvider { get; }

    protected readonly Dictionary<FcsProjectKey, FcsModuleCapturedInfo> ModuleCaches = new();
    protected readonly Dictionary<IPsiModule, FcsModuleCapturedInfo> ScriptCaches = new();

    private readonly OneToSetMap<FcsProjectKey, FcsProjectKey> myReferencingModules = new();
    private readonly ISet<IPsiSourceFile> myDirtyFiles = new HashSet<IPsiSourceFile>();

    public FcsCapturedInfoCache(Lifetime lifetime, IFcsProjectProvider fcsProjectProvider,
      FSharpScriptPsiModulesProvider scriptPsiModulesProvider, IShellLocks locks)
    {
      myLocks = locks;
      FcsProjectProvider = fcsProjectProvider;

      fcsProjectProvider.ProjectRemoved.Advise(lifetime, RemoveProject);
      scriptPsiModulesProvider.ModuleInvalidated.Advise(lifetime, InvalidateScript);
    }

    private static bool IsFSharpFile(IPsiSourceFile sourceFile) =>
      sourceFile.LanguageType.Is<FSharpProjectFileType>();

    private void RemoveProject([NotNull] Tuple<FcsProjectKey, FcsProject> invalidated)
    {
      var (projectKey, _) = invalidated;
      Invalidate(projectKey);
    }

    private void InvalidateScript(IPsiModule psiModule)
    {
      myLocks.AssertWriteAccessAllowed();
      ScriptCaches.Remove(psiModule);
    }

    private void Invalidate(FcsProjectKey projectKey)
    {
      InvalidateReferencingModules(projectKey);
      if (ModuleCaches.TryGetValue(projectKey, out var symbols) && symbols.FcsProject is { } fcsProject)
      {
        foreach (var referencedProjectKey in fcsProject.ReferencedModules)
        {
          myReferencingModules.Remove(referencedProjectKey, projectKey);
        }
      }

      ModuleCaches.Remove(projectKey);
    }

    private void InvalidateReferencingModules(FcsProjectKey projectKey)
    {
      foreach (var referencingModule in myReferencingModules[projectKey])
      {
        if (ModuleCaches.TryGetValue(referencingModule, out var moduleSymbols))
          moduleSymbols.Invalidate();
      }
    }

    protected virtual void Invalidate(IPsiSourceFile sourceFile)
    {
      var psiModule = sourceFile.PsiModule;
      if (psiModule is FSharpScriptPsiModule)
      {
        ScriptCaches.Remove(psiModule);
        return;
      }

      if (psiModule.ContainingProjectModule is not IProject)
        return;

      var projectKey = FcsProjectKey.Create(psiModule);

      if (!psiModule.IsValid())
      {
        Invalidate(projectKey);
        return;
      }

      if (ModuleCaches.TryGetValue(projectKey, out var moduleCache))
        moduleCache.Invalidate(sourceFile);

      InvalidateReferencingModules(projectKey);
    }

    public void MarkAsDirty(IPsiSourceFile sourceFile)
    {
      myLocks.AssertWriteAccessAllowed();

      myDirtyFiles.Add(sourceFile);
    }

    public object Load(IProgressIndicator progress, bool enablePersistence) => null;

    public void MergeLoaded(object data)
    {
    }

    public void Save(IProgressIndicator progress, bool enablePersistence)
    {
    }

    public bool UpToDate(IPsiSourceFile sourceFile)
    {
      myLocks.AssertReadAccessAllowed();

      lock (mySyncObj)
        return !myDirtyFiles.Contains(sourceFile);
    }

    public object Build(IPsiSourceFile sourceFile, bool isStartup) => null;

    public void Merge(IPsiSourceFile sourceFile, object builtPart)
    {
    }

    public void Drop(IPsiSourceFile sourceFile)
    {
      myLocks.AssertWriteAccessAllowed();

      if (ModuleCaches.IsEmpty())
        return;

      Invalidate(sourceFile);
    }

    public void OnDocumentChange(IPsiSourceFile sourceFile, ProjectFileDocumentCopyChange change) =>
      MarkAsDirty(sourceFile);

    public void OnPsiChange(ITreeNode elementContainingChanges, PsiChangedElementType type)
    {
      if (elementContainingChanges == null)
        return;
      if (ContentModelFork.IsCurrentlyForked)
        return;

      var sourceFile = elementContainingChanges.GetSourceFile();
      Assertion.Assert(sourceFile != null);

      MarkAsDirty(sourceFile);
    }

    public void SyncUpdate(bool underTransaction)
    {
      using var writeCookie = WriteLockCookie.Create(underTransaction);
      using var lockCookie = MonitorInterruptibleCookie.EnterOrThrow(mySyncObj);

      if (ModuleCaches.IsEmpty())
      {
        myDirtyFiles.Clear();
        return;
      }

      foreach (var sourceFile in myDirtyFiles)
      {
        if (!sourceFile.IsValid())
          continue;

        if (IsFSharpFile(sourceFile))
          Invalidate(sourceFile);
        else
        {
          var psiModule = sourceFile.PsiModule;
          if (psiModule.ContainingProjectModule is IProject)
          {
            var projectKey = FcsProjectKey.Create(psiModule);
            InvalidateReferencingModules(projectKey);  
          }
          
        }
      }

      myDirtyFiles.Clear();
    }

    public void Dump(TextWriter writer, IPsiSourceFile sourceFile)
    {
    }

    public bool HasDirtyFiles
    {
      get
      {
        myLocks.AssertReadAccessAllowed();
        lock (mySyncObj)
          return !myDirtyFiles.IsEmpty();
      }
    }

    public IFcsFileCapturedInfo GetOrCreateFileCapturedInfo(IPsiSourceFile sourceFile) =>
      GetModuleCapturedInfo(sourceFile).GetResolvedSymbols(sourceFile);

    [NotNull]
    private FcsModuleCapturedInfo GetModuleCapturedInfo(IPsiSourceFile sourceFile)
    {
      myLocks.AssertReadAccessAllowed();
      SyncUpdate(false);

      var psiModule = sourceFile.PsiModule;
      if (psiModule.IsMiscFilesProjectModule() && psiModule is not SandboxPsiModule)
        return FcsModuleCapturedInfo.Empty;

      lock (mySyncObj)
      {
        if (psiModule is FSharpScriptPsiModule)
        {
          var scriptInfo = new FcsModuleCapturedInfo(null, true);
          ScriptCaches[psiModule] = scriptInfo;
          return scriptInfo;
        }

        if (psiModule.ContainingProjectModule is not IProject)
          return new FcsModuleCapturedInfo(null);

        var projectKey = FcsProjectKey.Create(psiModule);
        if (ModuleCaches.TryGetValue(projectKey, out var info))
          return info;

        var fcsProject = FcsProjectProvider.GetFcsProject(psiModule)?.Value;
        if (fcsProject == null)
          return FcsModuleCapturedInfo.Empty;

        var moduleInfo = new FcsModuleCapturedInfo(fcsProject);
        ModuleCaches[projectKey] = moduleInfo;

        // todo: fix invalidating F# -> C# -> F# modules
        foreach (var referencedModule in fcsProject.ReferencedModules)
          myReferencingModules.Add(referencedModule, projectKey);

        return moduleInfo;
      }
    }
  }
}
