using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.DocumentManagers.impl;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  [SolutionComponent]
  public class FSharpResolvedSymbolsCache : ICache, IFSharpResolvedSymbolsCache
  {
    public IPsiModules PsiModules { get; }
    public FSharpCheckerService CheckerService { get; }
    public IFSharpProjectOptionsProvider ProjectOptionsProvider { get; }

    private readonly object myLock = new object();
    private readonly ISet<IPsiSourceFile> myDirtyFiles = new HashSet<IPsiSourceFile>();

    public FSharpResolvedSymbolsCache(Lifetime lifetime, FSharpCheckerService checkerService, IPsiModules psiModules,
      IFSharpProjectOptionsProvider projectOptionsProvider)
    {
      PsiModules = psiModules;
      CheckerService = checkerService;
      ProjectOptionsProvider = projectOptionsProvider;

      projectOptionsProvider.ModuleInvalidated.Advise(lifetime, Invalidate);
    }

    private static bool IsApplicable(IPsiSourceFile sourceFile) =>
      sourceFile.LanguageType.Is<FSharpProjectFileType>();

    public void Invalidate(IPsiModule psiModule)
    {
      lock (myLock)
      {
        myPsiModules.Remove(psiModule);
        InvalidateReferencingModules(psiModule);
      }
    }

    private void InvalidateReferencingModules(IPsiModule psiModule)
    {
      using (CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule()))
      {
        var resolveContext = CompilationContextCookie.GetContext();
        foreach (var psiModuleReference in PsiModules.GetReverseModuleReferences(psiModule, resolveContext))
          if (myPsiModules.TryGetValue(psiModuleReference.Module, out var moduleSymbols))
            moduleSymbols.Invalidate();
      }
    }

    private void Invalidate(IPsiSourceFile sourceFile)
    {
      var psiModule = sourceFile.PsiModule;
      if (myPsiModules.TryGetValue(psiModule, out var moduleResolvedSymbols))
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
        Invalidate(sourceFile);
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

    private readonly IDictionary<IPsiModule, FSharpModuleResolvedSymbols> myPsiModules =
      new Dictionary<IPsiModule, FSharpModuleResolvedSymbols>();

    private IFSharpFileResolvedSymbols GetOrCreateResolvedSymbols(IPsiSourceFile sourceFile) =>
      GetModuleResolvedSymbols(sourceFile).GetResolvedSymbols(sourceFile);

    [NotNull]
    private IFSharpModuleResolvedSymbols GetModuleResolvedSymbols(IPsiSourceFile sourceFile)
    {
      var psiModule = sourceFile.PsiModule;
      if (psiModule.IsMiscFilesProjectModule())
        return FSharpMiscModuleResolvedSymbols.Instance;

      lock (myLock)
      {
        if (HasDirtyFiles)
          InvalidateDirty();

        if (myPsiModules.TryGetValue(psiModule, out var symbols))
          return symbols;

        var parsingOptions = ProjectOptionsProvider.GetParsingOptions(sourceFile);
        var filesCount = parsingOptions.SourceFiles.Length;

        var moduleResolvedSymbols =
          new FSharpModuleResolvedSymbols(psiModule, filesCount, CheckerService, ProjectOptionsProvider);
        myPsiModules[psiModule] = moduleResolvedSymbols;
        return moduleResolvedSymbols;
      }
    }

    public FSharpSymbolUse GetSymbolUse(IPsiSourceFile sourceFile, int offset) =>
      GetOrCreateResolvedSymbols(sourceFile).GetSymbolUse(offset);

    public FSharpSymbol GetSymbolDeclaration(IPsiSourceFile sourceFile, int offset) =>
      GetOrCreateResolvedSymbols(sourceFile).GetSymbolDeclaration(offset);

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllDeclaredSymbols(IPsiSourceFile sourceFile) =>
      GetOrCreateResolvedSymbols(sourceFile).GetAllDeclaredSymbols();

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllResolvedSymbols(IPsiSourceFile sourceFile) =>
      GetOrCreateResolvedSymbols(sourceFile).GetAllResolvedSymbols();
  }
}
