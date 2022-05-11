using System.Collections.Generic;
using System.IO;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
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
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  [SolutionComponent]
  public class FcsResolvedSymbolsCache : IPsiSourceFileCache, IFcsResolvedSymbolsCache
  {
    public IPsiModules PsiModules { get; }
    public FcsCheckerService CheckerService { get; }
    public IFcsProjectProvider FcsProjectProvider { get; }

    protected readonly IDictionary<IPsiModule, FcsModuleResolvedSymbols> PsiModulesCaches =
      new Dictionary<IPsiModule, FcsModuleResolvedSymbols>();

    private readonly OneToSetMap<IPsiModule, IPsiModule> myReferencingModules = new();
    private readonly ISet<IPsiSourceFile> myDirtyFiles = new HashSet<IPsiSourceFile>();

    public FcsResolvedSymbolsCache(Lifetime lifetime, FcsCheckerService checkerService, IPsiModules psiModules,
      IFcsProjectProvider fcsProjectProvider, AssemblyReaderShim assemblyReaderShim)
    {
      PsiModules = psiModules;
      CheckerService = checkerService;
      FcsProjectProvider = fcsProjectProvider;

      fcsProjectProvider.ModuleInvalidated.Advise(lifetime, Invalidate);
      assemblyReaderShim.ModuleInvalidated.Advise(lifetime, Invalidate);
    }

    private static bool IsApplicable(IPsiSourceFile sourceFile) =>
      sourceFile.LanguageType.Is<FSharpProjectFileType>();

    public void Invalidate(IPsiModule psiModule)
    {
      using var _ = FcsReadWriteLock.WriteCookie.Create();
      {
        InvalidateReferencingModules(psiModule);
        if (PsiModulesCaches.TryGetValue(psiModule, out var symbols) && symbols.FcsProject is { } fcsProject)
        {
          foreach (var referencedModule in fcsProject.ReferencedModules)
            myReferencingModules.Remove(referencedModule, psiModule);
        }

        PsiModulesCaches.Remove(psiModule);
      }
    }

    private void InvalidateReferencingModules(IPsiModule psiModule)
    {
      if (PsiModulesCaches.IsEmpty())
        return;

      foreach (var referencingModule in myReferencingModules.GetReadOnlyValues(psiModule))
      {
        if (PsiModulesCaches.TryGetValue(referencingModule, out var moduleSymbols))
          moduleSymbols.Invalidate();
      }
    }

    protected virtual void Invalidate(IPsiSourceFile sourceFile)
    {
      var psiModule = sourceFile.PsiModule;
      if (!psiModule.IsValid())
      {
        Invalidate(psiModule);
        return;
      }

      if (PsiModulesCaches.TryGetValue(psiModule, out var moduleResolvedSymbols))
        moduleResolvedSymbols.Invalidate(sourceFile);

      InvalidateReferencingModules(psiModule);
    }

    public void MarkAsDirty(IPsiSourceFile sourceFile)
    {
      if (!IsApplicable(sourceFile))
        return;

      using var _ = FcsReadWriteLock.WriteCookie.Create();
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
      using var _ = FcsReadWriteLock.ReadCookie.Create();
        return !myDirtyFiles.Contains(sourceFile);
    }

    public object Build(IPsiSourceFile sourceFile, bool isStartup) => null;

    public void Merge(IPsiSourceFile sourceFile, object builtPart)
    {
    }

    public void Drop(IPsiSourceFile sourceFile)
    {
      using var _ = FcsReadWriteLock.WriteCookie.Create();
      {
        if (PsiModulesCaches.IsEmpty())
          return;

        Invalidate(sourceFile);
      }
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

      myDirtyFiles.Clear();
    }

    public void SyncUpdate(bool underTransaction)
    {
      using var _ = FcsReadWriteLock.WriteCookie.Create();
        InvalidateDirty();
    }

    public void Dump(TextWriter writer, IPsiSourceFile sourceFile)
    {
    }

    public bool HasDirtyFiles
    {
      get
      {
        using var _ = FcsReadWriteLock.ReadCookie.Create();
          return !myDirtyFiles.IsEmpty();
      }
    }

    private IFcsFileResolvedSymbols GetOrCreateResolvedSymbols(IPsiSourceFile sourceFile) =>
      GetModuleResolvedSymbols(sourceFile).GetResolvedSymbols(sourceFile);

    [NotNull]
    private FcsModuleResolvedSymbols GetModuleResolvedSymbols(IPsiSourceFile sourceFile)
    {
      var psiModule = sourceFile.PsiModule;
      if (psiModule.IsMiscFilesProjectModule() && !(psiModule is SandboxPsiModule))
        return FcsModuleResolvedSymbols.Empty;

      using var _ = FcsReadWriteLock.WriteCookie.Create();
      {
        FcsProjectProvider.InvalidateDirty();

        if (HasDirtyFiles)
          InvalidateDirty();

        if (PsiModulesCaches.TryGetValue(psiModule, out var symbols))
          return symbols;

        if (psiModule is FSharpScriptPsiModule)
        {
          var scriptResolvedSymbols = new FcsModuleResolvedSymbols(null, true);
          PsiModulesCaches[psiModule] = scriptResolvedSymbols;
          return scriptResolvedSymbols;
        }

        var fcsProject = FcsProjectProvider.GetFcsProject(psiModule)?.Value;
        if (fcsProject == null)
          return FcsModuleResolvedSymbols.Empty;

        var moduleResolvedSymbols = new FcsModuleResolvedSymbols(fcsProject);
        PsiModulesCaches[psiModule] = moduleResolvedSymbols;

        foreach (var referencedModule in fcsProject.ReferencedModules)
          myReferencingModules.Add(referencedModule, psiModule);

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
