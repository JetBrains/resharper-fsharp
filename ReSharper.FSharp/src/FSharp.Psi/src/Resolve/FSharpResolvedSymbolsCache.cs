using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Application.Progress;
using JetBrains.DocumentManagers.impl;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Build;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Threading;
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
    public OutputAssemblies OutputAssemblies { get; }

    public FSharpResolvedSymbolsCache(Lifetime lifetime, FSharpCheckerService checkerService, IPsiModules psiModules,
      IFSharpProjectOptionsProvider projectOptionsProvider, OutputAssemblies outputAssemblies, ChangeManager changeManager)
    {
      PsiModules = psiModules;
      CheckerService = checkerService;
      ProjectOptionsProvider = projectOptionsProvider;
      OutputAssemblies = outputAssemblies;

      projectOptionsProvider.ModuleInvalidated.Advise(lifetime, Invalidate);
      changeManager.Changed2.Advise(lifetime, ProcessChange);
    }

    private void ProcessChange(ChangeEventArgs args)
    {
      var projectOutputChange = args.ChangeMap.GetChange<ProjectOutputChange>(OutputAssemblies);
      if (projectOutputChange == null)
        return;

      var changes = projectOutputChange.Changes;
    }

    private readonly JetFastSemiReenterableRWLock myLock = new JetFastSemiReenterableRWLock();

    public void Invalidate(IPsiModule psiModule)
    {
      using (myLock.UsingWriteLock())
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
        {
          if (myPsiModules.TryGetValue(psiModuleReference.Module, out var moduleSymbols))
            moduleSymbols.Invalidate();
        }
      }
    }

    private void Invalidate(IPsiSourceFile sourceFile)
    {
      using (myLock.UsingWriteLock())
      {
        var psiModule = sourceFile.PsiModule;
        if (myPsiModules.TryGetValue(psiModule, out var moduleResolvedSymbols))
          moduleResolvedSymbols.Invalidate(sourceFile);

        InvalidateReferencingModules(psiModule);
      }
    }

    public void MarkAsDirty(IPsiSourceFile sourceFile) => Invalidate(sourceFile);

    public object Load(IProgressIndicator progress, bool enablePersistence) => null;

    public void MergeLoaded(object data)
    {
    }

    public void Save(IProgressIndicator progress, bool enablePersistence)
    {
    }

    public bool UpToDate(IPsiSourceFile sourceFile) => true;

    public object Build(IPsiSourceFile sourceFile, bool isStartup) => null;

    public void Merge(IPsiSourceFile sourceFile, object builtPart)
    {
    }

    public void Drop(IPsiSourceFile sourceFile) => Invalidate(sourceFile);

    public void OnDocumentChange(IPsiSourceFile sourceFile, ProjectFileDocumentCopyChange change) =>
      Invalidate(sourceFile);

    public void OnPsiChange(ITreeNode elementContainingChanges, PsiChangedElementType type)
    {
      // todo
    }


    public void SyncUpdate(bool underTransaction)
    {
    }

    public void Dump(TextWriter writer, IPsiSourceFile sourceFile)
    {
    }

    public bool HasDirtyFiles => false;

    // todo: misc files project?
    private readonly IDictionary<IPsiModule, FSharpModuleResolvedSymbols> myPsiModules =
      new Dictionary<IPsiModule, FSharpModuleResolvedSymbols>();

    private FSharpFileResolvedSymbols GetOrCreateResolvedSymbols(IPsiSourceFile sourceFile) =>
      GetModuleResolvedSymbols(sourceFile).GetResolvedSymbols(sourceFile);

    [NotNull]
    private IFSharpModuleResolvedSymbols GetModuleResolvedSymbols(IPsiSourceFile sourceFile)
    {
      var psiModule = sourceFile.PsiModule;
      if (sourceFile.PsiModule.IsMiscFilesProjectModule())
        return FSharpMiscModuleResolvedSymbols.Instance;

      using (myLock.UsingReadLock())
      {
        if (myPsiModules.TryGetValue(psiModule, out var symbols))
          return symbols;
      }

      using (myLock.UsingWriteLock())
      {
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

    public FSharpSymbolUse GetSymbolUse(IPsiSourceFile sourceFile, int offset)
    {
      var resolvedSymbols = GetOrCreateResolvedSymbols(sourceFile);

      var resolvedSymbol = resolvedSymbols.Uses.TryGetValue(offset);
      if (resolvedSymbol == null)
        return null;

      return resolvedSymbols.Declarations.TryGetValue(offset) == null
        ? resolvedSymbol.SymbolUse
        : null;
    }

    public FSharpSymbol GetSymbolDeclaration(IPsiSourceFile sourceFile, int offset)
    {
      var resolvedSymbols = GetOrCreateResolvedSymbols(sourceFile);
      return resolvedSymbols.Declarations.TryGetValue(offset)?.SymbolUse.Symbol;
    }

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllDeclaredSymbols(IPsiSourceFile sourceFile)
    {
      var resolvedSymbols = GetOrCreateResolvedSymbols(sourceFile);
      return resolvedSymbols.Declarations.Values.AsChunkIReadOnlyList();
    }

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllResolvedSymbols(IPsiSourceFile sourceFile)
    {
      var resolvedSymbols = GetOrCreateResolvedSymbols(sourceFile);
      return resolvedSymbols.Uses.Values.AsChunkIReadOnlyList();
    }
  }
}
