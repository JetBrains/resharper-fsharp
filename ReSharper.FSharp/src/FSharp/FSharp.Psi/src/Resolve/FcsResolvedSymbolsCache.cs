using System;
using System.Collections.Generic;
using System.IO;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
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
using JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader;
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
  public class FcsResolvedSymbolsCache : IPsiSourceFileCache, IFcsResolvedSymbolsCache
  {
    private readonly object mySyncObj = new();
    private readonly IShellLocks myLocks;
    public IPsiModules PsiModules { get; }
    public FcsCheckerService CheckerService { get; }
    public IFcsProjectProvider FcsProjectProvider { get; }

    protected readonly Dictionary<FcsProjectKey, FcsModuleResolvedSymbols> ProjectSymbolsCaches = new();
    protected readonly Dictionary<IPsiModule, FcsModuleResolvedSymbols> ScriptCaches = new();

    private readonly OneToSetMap<FcsProjectKey, FcsProjectKey> myReferencingModules = new();
    private readonly ISet<IPsiSourceFile> myDirtyFiles = new HashSet<IPsiSourceFile>();

    public FcsResolvedSymbolsCache(Lifetime lifetime, FcsCheckerService checkerService, IPsiModules psiModules,
      IFcsProjectProvider fcsProjectProvider, FSharpScriptPsiModulesProvider scriptPsiModulesProvider,
      IShellLocks locks)
    {
      myLocks = locks;
      PsiModules = psiModules;
      CheckerService = checkerService;
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
      if (ProjectSymbolsCaches.TryGetValue(projectKey, out var symbols) && symbols.FcsProject is { } fcsProject)
      {
        foreach (var referencedProjectKey in fcsProject.ReferencedModules)
        {
          myReferencingModules.Remove(referencedProjectKey, projectKey);
        }
      }

      ProjectSymbolsCaches.Remove(projectKey);
    }

    private void InvalidateReferencingModules(FcsProjectKey projectKey)
    {
      foreach (var referencingModule in myReferencingModules[projectKey])
      {
        if (ProjectSymbolsCaches.TryGetValue(referencingModule, out var moduleSymbols))
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

      if (ProjectSymbolsCaches.TryGetValue(projectKey, out var moduleResolvedSymbols))
        moduleResolvedSymbols.Invalidate(sourceFile);

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

      if (ProjectSymbolsCaches.IsEmpty())
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

      if (ProjectSymbolsCaches.IsEmpty())
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

    private IFcsFileResolvedSymbols GetOrCreateResolvedSymbols(IPsiSourceFile sourceFile) =>
      GetModuleResolvedSymbols(sourceFile).GetResolvedSymbols(sourceFile);

    [NotNull]
    private FcsModuleResolvedSymbols GetModuleResolvedSymbols(IPsiSourceFile sourceFile)
    {
      myLocks.AssertReadAccessAllowed();
      SyncUpdate(false);

      var psiModule = sourceFile.PsiModule;
      if (psiModule.IsMiscFilesProjectModule() && psiModule is not SandboxPsiModule)
        return FcsModuleResolvedSymbols.Empty;

      lock (mySyncObj)
      {
        if (psiModule is FSharpScriptPsiModule)
        {
          var scriptResolvedSymbols = new FcsModuleResolvedSymbols(null, true);
          ScriptCaches[psiModule] = scriptResolvedSymbols;
          return scriptResolvedSymbols;
        }

        if (psiModule.ContainingProjectModule is not IProject)
          return new FcsModuleResolvedSymbols(null);

        var projectKey = FcsProjectKey.Create(psiModule);
        if (ProjectSymbolsCaches.TryGetValue(projectKey, out var symbols))
          return symbols;

        var fcsProject = FcsProjectProvider.GetFcsProject(psiModule)?.Value;
        if (fcsProject == null)
          return FcsModuleResolvedSymbols.Empty;

        var moduleResolvedSymbols = new FcsModuleResolvedSymbols(fcsProject);
        ProjectSymbolsCaches[projectKey] = moduleResolvedSymbols;

        // todo: fix invalidating F# -> C# -> F# modules
        foreach (var referencedModule in fcsProject.ReferencedModules)
          myReferencingModules.Add(referencedModule, projectKey);

        return moduleResolvedSymbols;
      }
    }

    public FSharpSymbolUse GetSymbolUse(IPsiSourceFile sourceFile, int offset) =>
      GetOrCreateResolvedSymbols(sourceFile).GetSymbolUse(offset);

    public FSharpSymbolUse GetSymbolDeclaration(IPsiSourceFile sourceFile, int offset) =>
      GetOrCreateResolvedSymbols(sourceFile).GetSymbolDeclaration(offset);

    public IReadOnlyList<FcsResolvedSymbolUse> GetAllDeclaredSymbols(IPsiSourceFile sourceFile) =>
      GetOrCreateResolvedSymbols(sourceFile).GetAllDeclaredSymbols();

    public IReadOnlyList<FcsResolvedSymbolUse> GetAllResolvedSymbols(IPsiSourceFile sourceFile) =>
      GetOrCreateResolvedSymbols(sourceFile).GetAllResolvedSymbols();

    public FSharpSymbol GetSymbol(IPsiSourceFile sourceFile, int offset) =>
      GetOrCreateResolvedSymbols(sourceFile).GetSymbol(offset);
  }
}
