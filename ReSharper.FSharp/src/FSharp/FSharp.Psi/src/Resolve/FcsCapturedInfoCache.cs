using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using JetBrains.Application.ContentModel;
using JetBrains.Application.Parts;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Collections.Synchronized;
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
  public class FcsCapturedInfoCache : IPsiSourceFileCacheWithForksSupport, IFcsCapturedInfoCache
  {
    private readonly FSharpScriptPsiModulesProvider myScriptPsiModulesProvider;
    private readonly IShellLocks myLocks;

    private readonly LazyForkedCopyOnWriteContentModelData<State> myState = new
    (
      factory: static () => new State(),
      // We intentionally create an empty State instance instead of copying the existing data. This is correct because:
      // - During Read operations:
      //       We only need to reuse the non-forked data from the shared state.
      // - During Write operations:
      //       We accept that the forked cache becomes empty and will be computed on-demand.
      //       It should not require much time, as most of the data for resolution is cached by the FCS.
      copyFactory: static _ => new State()
    );

    protected sealed class State
    {
      public readonly Dictionary<FcsProjectKey, FcsModuleCapturedInfo> ModuleCaches = new();
      public readonly Dictionary<IPsiModule, FcsModuleCapturedInfo> ScriptCaches = new();
      public readonly OneToSetMap<FcsProjectKey, FcsProjectKey> ReferencingModules = new();
      public readonly SynchronizedSet<IPsiSourceFile> DirtyFiles = [];
    }

    public IFcsProjectProvider FcsProjectProvider { get; }

    /// Read-only forks are used by SWA and can compute missing FCS data accessible outside the fork
    /// since they cannot modify it further.
    private static bool IsReadonlyFork => 
      ContentModelFork.IsCurrentlyForked &&
      !ContentModelFork.CurrentCapabilities.HasFlag(ContentModelForkCapabilities.WriteOperations);

    public FcsCapturedInfoCache(Lifetime lifetime, IFcsProjectProvider fcsProjectProvider,
      FSharpScriptPsiModulesProvider scriptPsiModulesProvider, IShellLocks locks)
    {
      myScriptPsiModulesProvider = scriptPsiModulesProvider;
      myLocks = locks;
      FcsProjectProvider = fcsProjectProvider;

      fcsProjectProvider.ProjectRemoved.Advise(lifetime, RemoveProject);
      scriptPsiModulesProvider.ModuleInvalidated.Advise(lifetime, x => InvalidateScript(x.PsiModule, x.IsRemoved));
    }

    private static bool IsFSharpFile(IPsiSourceFile sourceFile) =>
      sourceFile.LanguageType.Is<FSharpProjectFileType>();

    private void RemoveProject([NotNull] Tuple<FcsProjectKey, FcsProject> invalidated)
    {
      var (projectKey, _) = invalidated;
      Invalidate(projectKey);
    }

    private void InvalidateScript(FSharpScriptPsiModule scriptPsiModule, bool isRemoved)
    {
      var state = myState.ValueForWrite;

      if (isRemoved)
      {
        myLocks.AssertWriteAccessAllowed();
        state.ScriptCaches.Remove(scriptPsiModule);
      }

      else if (state.ScriptCaches.TryGetValue(scriptPsiModule, out var symbols))
        symbols.Invalidate(scriptPsiModule.SourceFile);

      InvalidateDirectReferencingScripts(scriptPsiModule, [scriptPsiModule]);
    }

    private void InvalidateDirectReferencingScripts(FSharpScriptPsiModule psiModule,
      HashSet<FSharpScriptPsiModule> visited)
    {
      var referencedByScripts = myScriptPsiModulesProvider.GetDirectReferencingScripts(psiModule);
      foreach (var script in referencedByScripts)
      {
        if (!visited.Add(script)) continue;

        if (!myState.ValueForWrite.ScriptCaches.TryGetValue(script, out var symbols)) continue;
        symbols.Invalidate(psiModule.SourceFile);

        InvalidateDirectReferencingScripts(script, visited);
      }
    }

    private void Invalidate(FcsProjectKey projectKey)
    {
      myLocks.AssertWriteAccessAllowed();
      var state = myState.ValueForWrite;

      InvalidateReferencingModules(projectKey, state);

      if (state.ModuleCaches.TryGetValue(projectKey, out var symbols) && symbols.FcsProject is { } fcsProject)
      {
        foreach (var referencedProjectKey in fcsProject.ReferencedModules)
        {
          state.ReferencingModules.Remove(referencedProjectKey, projectKey);
        }
      }

      state.ModuleCaches.Remove(projectKey);
    }

    private static void InvalidateReferencingModules(FcsProjectKey projectKey, State state)
    {
      foreach (var referencingModule in state.ReferencingModules[projectKey])
      {
        if (state.ModuleCaches.TryGetValue(referencingModule, out var moduleSymbols))
        {
          moduleSymbols.Invalidate();
        }
      }
    }

    protected virtual void Invalidate(IPsiSourceFile sourceFile, State state)
    {
      var psiModule = sourceFile.PsiModule;
      if (psiModule is FSharpScriptPsiModule scriptPsiModule)
      {
        InvalidateScript(scriptPsiModule, isRemoved: false);
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

      if (state.ModuleCaches.TryGetValue(projectKey, out var moduleCache))
      {
        moduleCache.Invalidate(sourceFile);
      }

      InvalidateReferencingModules(projectKey, state);
    }

    public void MarkAsDirty(IPsiSourceFile sourceFile)
    {
      myLocks.AssertWriteAccessAllowed();

      myState.ValueForWrite.DirtyFiles.Add(sourceFile);
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

      return !myState.ValueForRead.DirtyFiles.Contains(sourceFile);
    }

    public object Build(IPsiSourceFile sourceFile, bool isStartup) => null;

    public void Merge(IPsiSourceFile sourceFile, object builtPart)
    {
    }

    public void MergeInFork(IPsiSourceFile sourceFile, object builtPart)
    {
    }

    public void Drop(IPsiSourceFile sourceFile)
    {
      myLocks.AssertWriteAccessAllowed();

      if (myState.ValueForRead.ModuleCaches.IsEmpty())
        return;

      Invalidate(sourceFile, myState.ValueForWrite);
    }

    public void OnDocumentChange(IPsiSourceFile sourceFile, ProjectFileDocumentCopyChange change) =>
      MarkAsDirty(sourceFile);

    public void OnPsiChange(ITreeNode elementContainingChanges, PsiChangedElementType type)
    {
      if (elementContainingChanges == null)
        return;

      var sourceFile = elementContainingChanges.GetSourceFile();
      Assertion.Assert(sourceFile != null);

      MarkAsDirty(sourceFile);
    }

    public void SyncUpdate(bool underTransaction)
    {
      if (myState.ValueForRead.DirtyFiles.IsEmpty())
        return;

      var state = myState.ValueForWrite;
      using var writeCookie = WriteLockCookie.Create(takeLock: underTransaction);
      using var lockCookie = MonitorInterruptibleCookie.EnterOrThrow(state);

      if (state.ModuleCaches.IsEmpty() && state.ScriptCaches.IsEmpty())
      {
        state.DirtyFiles.Clear();
        return;
      }

      foreach (var sourceFile in state.DirtyFiles)
      {
        if (!sourceFile.IsValid())
          continue;

        if (IsFSharpFile(sourceFile))
        {
          Invalidate(sourceFile, state);
        }
        else
        {
          var psiModule = sourceFile.PsiModule;
          if (psiModule.ContainingProjectModule is IProject)
          {
            var projectKey = FcsProjectKey.Create(psiModule);
            InvalidateReferencingModules(projectKey, state);
          }
        }
      }

      state.DirtyFiles.Clear();
    }

    public void Dump(TextWriter writer, IPsiSourceFile sourceFile)
    {
    }

    public bool HasDirtyFiles
    {
      get
      {
        myLocks.AssertReadAccessAllowed();
        return !myState.ValueForRead.DirtyFiles.IsEmpty();
      }
    }

    private static IFcsFileCapturedInfo TryGetFileCapturedInfo(IPsiSourceFile sourceFile, FcsModuleCapturedInfo moduleCapturedInfo)
    {
      if (moduleCapturedInfo == null)
        return null;

      var canMutateSharedData = !ContentModelFork.IsCurrentlyForked || IsReadonlyFork;
      if (canMutateSharedData)
      {
        var resolvedSymbols = moduleCapturedInfo.TryGetResolvedSymbols(sourceFile);
        if (resolvedSymbols != null)
          return resolvedSymbols;

        // do not mutate the shared data, go do the State fork
      }
      else return moduleCapturedInfo.GetOrCreateResolvedSymbols(sourceFile);

      return null;
    }

    private IFcsFileCapturedInfo GetOrCreateScriptFileCapturedInfo(IPsiSourceFile sourceFile)
    {
      var psiModule = sourceFile.PsiModule;

      {
        var stateForRead = myState.ValueForRead;
        lock (stateForRead)
        {
          stateForRead.ScriptCaches.TryGetValue(psiModule, out var moduleInfo);

          if (TryGetFileCapturedInfo(sourceFile, moduleInfo) is { } fileInfo)
            return fileInfo;
        }
      }

      var stateForWrite = IsReadonlyFork ? myState.ValueForRead : myState.ValueForWrite;
      lock (stateForWrite)
      {
        if (!stateForWrite.ScriptCaches.TryGetValue(psiModule, out var moduleInfo))
        {
          moduleInfo = new FcsModuleCapturedInfo(null, true);
          stateForWrite.ScriptCaches[psiModule] = moduleInfo;
        }

        return moduleInfo.GetOrCreateResolvedSymbols(sourceFile);
      }
    }

    private IFcsFileCapturedInfo GetOrCreateProjectFileCapturedInfo(IPsiSourceFile sourceFile)
    {
      var psiModule = sourceFile.PsiModule;

      if (psiModule.ContainingProjectModule is not IProject)
        return EmptyFcsFileCapturedInfo.Instance;

      var projectKey = FcsProjectKey.Create(psiModule);

      {
        var stateForRead = myState.ValueForRead;
        lock (stateForRead)
        {
          stateForRead.ModuleCaches.TryGetValue(projectKey, out var moduleInfo);

          if (TryGetFileCapturedInfo(sourceFile, moduleInfo) is { } fileInfo)
            return fileInfo;
        }
      }

      var stateForWrite = IsReadonlyFork ? myState.ValueForRead : myState.ValueForWrite;
      lock (stateForWrite)
      {
        if (!stateForWrite.ModuleCaches.TryGetValue(projectKey, out var moduleInfo))
        {
          // todo: do not trigger compiler update
          if (FcsProjectProvider.GetFcsProject(psiModule) is not { Value: { } fcsProject })
            return EmptyFcsFileCapturedInfo.Instance;

          moduleInfo = new FcsModuleCapturedInfo(fcsProject);
          stateForWrite.ModuleCaches[projectKey] = moduleInfo;

          // todo: fix invalidating F# -> C# -> F# modules
          foreach (var referencedModule in fcsProject.ReferencedModules)
            stateForWrite.ReferencingModules.Add(referencedModule, projectKey);
        }

        return moduleInfo.GetOrCreateResolvedSymbols(sourceFile);
      }
    }

    public IFcsFileCapturedInfo GetOrCreateFileCapturedInfo(IPsiSourceFile sourceFile)
    {
      myLocks.AssertReadAccessAllowed();
      SyncUpdate(underTransaction: false);

      var psiModule = sourceFile.PsiModule;

      if (psiModule.IsMiscFilesProjectModule() && psiModule is not SandboxPsiModule)
        return EmptyFcsFileCapturedInfo.Instance;

      return psiModule is FSharpScriptPsiModule 
        ? GetOrCreateScriptFileCapturedInfo(sourceFile) 
        : GetOrCreateProjectFileCapturedInfo(sourceFile);
    }
  }
}
