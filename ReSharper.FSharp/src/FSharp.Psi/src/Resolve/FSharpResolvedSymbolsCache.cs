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
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files.SandboxFiles;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  [SolutionComponent]
  public class FSharpResolvedSymbolsCache : IPsiSourceFileCache, IFSharpResolvedSymbolsCache
  {
    public IPsiModules PsiModules { get; }
    public FcsCheckerService CheckerService { get; }
    public IFcsProjectProvider FcsProjectProvider { get; }

    private readonly object myLock = new object();
    private readonly ISet<IPsiSourceFile> myDirtyFiles = new HashSet<IPsiSourceFile>();

    public FSharpResolvedSymbolsCache(Lifetime lifetime, FcsCheckerService checkerService, IPsiModules psiModules,
      IFcsProjectProvider fcsProjectProvider)
    {
      PsiModules = psiModules;
      CheckerService = checkerService;
      FcsProjectProvider = fcsProjectProvider;

      fcsProjectProvider.ModuleInvalidated.Advise(lifetime, Invalidate);
    }

    private static bool IsApplicable(IPsiSourceFile sourceFile) =>
      sourceFile.LanguageType.Is<FSharpProjectFileType>();

    public void Invalidate(IPsiModule psiModule)
    {
      lock (myLock)
      {
        PsiModulesCaches.Remove(psiModule);
        if (psiModule.IsValid())
          InvalidateReferencingModules(psiModule);
      }
    }

    private void InvalidateReferencingModules(IPsiModule psiModule)
    {
      if (PsiModulesCaches.IsEmpty())
        return;

      // todo: reuse FcsProjectProvider references
      using (CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule()))
      {
        var resolveContext = CompilationContextCookie.GetContext();
        foreach (var psiModuleReference in PsiModules.GetReverseModuleReferences(psiModule, resolveContext))
          if (PsiModulesCaches.TryGetValue(psiModuleReference.Module, out var moduleSymbols))
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

      lock (myLock)
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
      lock (myLock)
        return !myDirtyFiles.Contains(sourceFile);
    }

    public object Build(IPsiSourceFile sourceFile, bool isStartup) => null;

    public void Merge(IPsiSourceFile sourceFile, object builtPart)
    {
    }

    public void Drop(IPsiSourceFile sourceFile)
    {
      lock (myLock)
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
      Assertion.Assert(sourceFile != null, "sourceFile != null");

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
      lock (myLock)
        InvalidateDirty();
    }

    public void Dump(TextWriter writer, IPsiSourceFile sourceFile)
    {
    }

    public bool HasDirtyFiles
    {
      get
      {
        lock (myLock)
          return !myDirtyFiles.IsEmpty();
      }
    }

    protected readonly IDictionary<IPsiModule, FSharpModuleResolvedSymbols> PsiModulesCaches =
      new Dictionary<IPsiModule, FSharpModuleResolvedSymbols>();

    private IFSharpFileResolvedSymbols GetOrCreateResolvedSymbols(IPsiSourceFile sourceFile) =>
      GetModuleResolvedSymbols(sourceFile).GetResolvedSymbols(sourceFile);

    [NotNull]
    private IFSharpModuleResolvedSymbols GetModuleResolvedSymbols(IPsiSourceFile sourceFile)
    {
      var psiModule = sourceFile.PsiModule;
      if (psiModule.IsMiscFilesProjectModule() && !(psiModule is SandboxPsiModule))
        return FSharpMiscModuleResolvedSymbols.Instance;

      FcsProjectProvider.InvalidateDirty();

      lock (myLock)
      {
        if (HasDirtyFiles)
          InvalidateDirty();

        if (PsiModulesCaches.TryGetValue(psiModule, out var symbols))
          return symbols;

        var parsingOptions = FcsProjectProvider.GetParsingOptions(sourceFile);
        var filesCount = parsingOptions.SourceFiles.Length;

        var moduleResolvedSymbols =
          new FSharpModuleResolvedSymbols(psiModule, filesCount, CheckerService, FcsProjectProvider);
        PsiModulesCaches[psiModule] = moduleResolvedSymbols;
        return moduleResolvedSymbols;
      }
    }

    public FSharpSymbolUse GetSymbolUse(IPsiSourceFile sourceFile, int offset) =>
      GetOrCreateResolvedSymbols(sourceFile).GetSymbolUse(offset);

    public FSharpSymbolUse GetSymbolDeclaration(IPsiSourceFile sourceFile, int offset) =>
      GetOrCreateResolvedSymbols(sourceFile).GetSymbolDeclaration(offset);

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllDeclaredSymbols(IPsiSourceFile sourceFile) =>
      GetOrCreateResolvedSymbols(sourceFile).GetAllDeclaredSymbols();

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllResolvedSymbols(IPsiSourceFile sourceFile) =>
      GetOrCreateResolvedSymbols(sourceFile).GetAllResolvedSymbols();

    public FSharpSymbol GetSymbol(IPsiSourceFile sourceFile, int offset) =>
      GetOrCreateResolvedSymbols(sourceFile).GetSymbol(offset);
  }
}
