using System;
using System.Collections.Generic;
using System.IO;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
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

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  [SolutionComponent]
  public class FcsResolvedSymbolsCache : IPsiSourceFileCache, IFcsResolvedSymbolsCache
  {
    private readonly object mySyncObj = new();
    private readonly IShellLocks myLocks;
    public IPsiModules PsiModules { get; }
    public FcsCheckerService CheckerService { get; }
    public IFcsProjectProvider FcsProjectProvider { get; }

    protected readonly Dictionary<FcsProjectKey, FcsModuleResolvedSymbols> PsiModulesCaches = new();

    private readonly OneToSetMap<FcsProjectKey, FcsProjectKey> myReferencingModules = new();
    private readonly ISet<IPsiSourceFile> myDirtyFiles = new HashSet<IPsiSourceFile>();
    private readonly ISet<IPsiModule> myDirtyNonFSharpModules = new HashSet<IPsiModule>();

    public FcsResolvedSymbolsCache(Lifetime lifetime, FcsCheckerService checkerService, IPsiModules psiModules,
      IFcsProjectProvider fcsProjectProvider, FSharpScriptPsiModulesProvider scriptPsiModulesProvider, IShellLocks locks)
    {
      myLocks = locks;
      PsiModules = psiModules;
      CheckerService = checkerService;
      FcsProjectProvider = fcsProjectProvider;

      fcsProjectProvider.ModuleInvalidated.Advise(lifetime, Invalidate);
      scriptPsiModulesProvider.ModuleInvalidated.Advise(lifetime, Invalidate);
    }

    private static bool IsApplicable(IPsiSourceFile sourceFile) =>
      sourceFile.LanguageType.Is<FSharpProjectFileType>();

    private void Invalidate(Tuple<IPsiModule, FcsProject> project)
    {
      var (psiModule, _) = project;
      Invalidate(psiModule);
    }

    private void Invalidate(IPsiModule psiModule)
    {
      myLocks.AssertWriteAccessAllowed();

      var projectKey = FcsProjectKey.Create(psiModule);

      InvalidateReferencingModules(psiModule);
      if (PsiModulesCaches.TryGetValue(projectKey, out var symbols) && symbols.FcsProject is { } fcsProject)
      {
        foreach (var referencedProjectKey in fcsProject.ReferencedModules)
        {
          myReferencingModules.Remove(referencedProjectKey, projectKey);
        }
      }

      PsiModulesCaches.Remove(projectKey);
    }

    private void InvalidateReferencingModules(IPsiModule psiModule)
    {
      myLocks.AssertWriteAccessAllowed();

      if (PsiModulesCaches.IsEmpty())
        return;

      var projectKey = FcsProjectKey.Create(psiModule);

      if (!PsiModulesCaches.TryGetValue(projectKey, out var symbols) || symbols.FcsProject is not { } fcsProject)
        return;

      foreach (var referencingModule in fcsProject.ReferencedModules)
      {
        if (PsiModulesCaches.TryGetValue(referencingModule, out var moduleSymbols))
          moduleSymbols.Invalidate();}
    }

    protected virtual void Invalidate(IPsiSourceFile sourceFile)
    {
      var psiModule = sourceFile.PsiModule;
      if (!psiModule.IsValid())
      {
        Invalidate(psiModule);
        return;
      }

      var projectKey = FcsProjectKey.Create(psiModule);
      if (PsiModulesCaches.TryGetValue(projectKey, out var moduleResolvedSymbols))
        moduleResolvedSymbols.Invalidate(sourceFile);

      InvalidateReferencingModules(psiModule);
    }

    public void MarkAsDirty(IPsiSourceFile sourceFile)
    {
      myLocks.AssertWriteAccessAllowed();

      if (IsApplicable(sourceFile))
        myDirtyFiles.Add(sourceFile);
      else
        myDirtyNonFSharpModules.Add(sourceFile.PsiModule);
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

      if (PsiModulesCaches.IsEmpty())
        return;

      Invalidate(sourceFile);
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


    private void InvalidateDirty()
    {
      foreach (var sourceFile in myDirtyFiles)
        Invalidate(sourceFile);

      foreach (var psiModule in myDirtyNonFSharpModules)
        InvalidateReferencingModules(psiModule);

      myDirtyFiles.Clear();
    }

    public void SyncUpdate(bool underTransaction)
    {
      using var cookie = WriteLockCookie.Create();

      InvalidateDirty();
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

      CheckerService.AssertFcsAccessThread();

      var psiModule = sourceFile.PsiModule;
      if (psiModule.IsMiscFilesProjectModule() && psiModule is not SandboxPsiModule)
        return FcsModuleResolvedSymbols.Empty;

      lock (mySyncObj)
      {
        var projectKey = FcsProjectKey.Create(psiModule);
        if (PsiModulesCaches.TryGetValue(projectKey, out var symbols))
          return symbols;

        if (psiModule is FSharpScriptPsiModule)
        {
          var scriptResolvedSymbols = new FcsModuleResolvedSymbols(null, true);
          PsiModulesCaches[projectKey] = scriptResolvedSymbols;
          return scriptResolvedSymbols;
        }

        var fcsProject = FcsProjectProvider.GetFcsProject(psiModule)?.Value;
        if (fcsProject == null)
          return FcsModuleResolvedSymbols.Empty;

        var moduleResolvedSymbols = new FcsModuleResolvedSymbols(fcsProject);
        PsiModulesCaches[projectKey] = moduleResolvedSymbols;

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
