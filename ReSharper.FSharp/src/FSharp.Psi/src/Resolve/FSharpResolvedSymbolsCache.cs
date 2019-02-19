using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.DocumentManagers.impl;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Build;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Threading;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using PrettyNaming = Microsoft.FSharp.Compiler.PrettyNaming;

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
    private FSharpModuleResolvedSymbols GetModuleResolvedSymbols(IPsiSourceFile sourceFile)
    {
      var psiModule = sourceFile.PsiModule;
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

  public class FSharpModuleResolvedSymbols
  {
    private readonly FSharpFileResolvedSymbols[] myFileResolvedSymbols;

    public IPsiModule PsiModule { get; }
    public FSharpCheckerService CheckerService { get; }
    public IFSharpProjectOptionsProvider ProjectOptionsProvider { get; }

    private readonly JetFastSemiReenterableRWLock myLock = new JetFastSemiReenterableRWLock();

    public FSharpModuleResolvedSymbols(IPsiModule psiModule, int filesCount, FSharpCheckerService checkerService,
      IFSharpProjectOptionsProvider projectOptionsProvider)
    {
      myFileResolvedSymbols = new FSharpFileResolvedSymbols[filesCount];

      CheckerService = checkerService;
      ProjectOptionsProvider = projectOptionsProvider;
      PsiModule = psiModule;
    }

    public void Invalidate(IPsiSourceFile sourceFile)
    {
      using (myLock.UsingWriteLock())
      {
        var fileIndex = ProjectOptionsProvider.GetFileIndex(sourceFile);
        var filesCount = myFileResolvedSymbols.Length;
        for (var i = fileIndex; i < filesCount; i++)
          myFileResolvedSymbols[i] = null;
      }
    }

    private FSharpFileResolvedSymbols TryGetResolvedSymbols(int fileIndex)
    {
      using (myLock.UsingReadLock())
        return myFileResolvedSymbols[fileIndex];
    }

    public FSharpFileResolvedSymbols GetResolvedSymbols(IPsiSourceFile sourceFile)
    {
      var fileIndex = ProjectOptionsProvider.GetFileIndex(sourceFile);
      var fileResolvedSymbols = TryGetResolvedSymbols(fileIndex);
      if (fileResolvedSymbols != null)
        return fileResolvedSymbols;

      using (myLock.UsingWriteLock())
      {
        fileResolvedSymbols = TryGetResolvedSymbols(fileIndex);
        if (fileResolvedSymbols != null)
          return fileResolvedSymbols;

        fileResolvedSymbols = CreateFileResolvedSymbols(sourceFile);
        myFileResolvedSymbols[fileIndex] = fileResolvedSymbols;
      }

      return fileResolvedSymbols;
    }

    [CanBeNull]
    private FSharpFileResolvedSymbols CreateFileResolvedSymbols(IPsiSourceFile sourceFile)
    {
      var interruptChecker = new SeldomInterruptCheckerWithCheckTime(100);
      // todo: cancellation
      if (!(sourceFile.GetPrimaryPsiFile() is IFSharpFile fsFile))
        return null;

      var checkResults = CheckerService.ParseAndCheckFile(sourceFile)?.Value.CheckResults;
      var symbolUses = checkResults?.GetAllUsesOfAllSymbolsInFile().RunAsTask();
      if (symbolUses == null)
        return null;

      var document = sourceFile.Document;
      var buffer = document.Buffer;
      var resolvedSymbols = new FSharpFileResolvedSymbols(symbolUses.Length);
      foreach (var symbolUse in symbolUses)
      {
        var symbol = symbolUse.Symbol;
        var range = symbolUse.RangeAlternate;

        var startOffset = document.GetOffset(range.Start);
        var endOffset = document.GetOffset(range.End);
        var mfv = symbol as FSharpMemberOrFunctionOrValue;
        var activePatternCase = symbol as FSharpActivePatternCase;

        if (symbolUse.IsFromDefinition)
        {
          if (mfv != null)
          {
            // workaround for auto-properties, see visualfsharp#3939
            var mfvLogicalName = mfv.LogicalName;
            if (mfvLogicalName.EndsWith("@", StringComparison.Ordinal))
              continue;

            // visualfsharp#3939
            if (mfvLogicalName == "v" &&
                resolvedSymbols.Declarations.ContainsKey(startOffset))
              continue;

            if (mfvLogicalName == StandardMemberNames.ClassConstructor)
              continue;

            // visualfsharp#3943, visualfsharp#3933
            if (mfvLogicalName != StandardMemberNames.Constructor &&
                !(fsFile.FindTokenAt(new TreeOffset(endOffset - 1)) is FSharpIdentifierToken || mfv.IsActivePattern))
              continue;
          }
          else if (activePatternCase != null)
          {
            // Skip active pattern cases bindings as these have incorrect ranges.
            // Active pattern cases uses inside bindings are currently marked as bindings so check the range.
            // https://github.com/Microsoft/visualfsharp/issues/4423
            if (activePatternCase.DeclarationLocation.Equals(range))
            {
              var activePatternId = fsFile.GetContainingNodeAt<ActivePatternId>(new TreeOffset(endOffset - 1));
              if (activePatternId == null)
                continue;

              var cases = activePatternId.Cases;
              var caseIndex = activePatternCase.Index;
              if (caseIndex < 0 || caseIndex >= cases.Count)
                continue;

              if (!(cases[caseIndex] is IActivePatternCaseDeclaration caseDeclaration))
                continue;

              var (caseStart, caseEnd) = caseDeclaration.GetTreeTextRange();
              var caseStartOffset = caseStart.Offset;
              var caseTextRange = new TextRange(caseStartOffset, caseEnd.Offset);
              resolvedSymbols.Declarations[caseStartOffset] = new FSharpResolvedSymbolUse(symbolUse, caseTextRange);
              continue;
            }

            var caseUseInBindingRange = new TextRange(startOffset, endOffset);
            resolvedSymbols.Uses[startOffset] = new FSharpResolvedSymbolUse(symbolUse, caseUseInBindingRange);
            continue;
          }
          else
          {
            // workaround for compiler generated symbols (e.g. fields auto-properties)
            if (!(fsFile.FindTokenAt(new TreeOffset(endOffset - 1)) is FSharpIdentifierToken))
              continue;
          }

          var textRange = new TextRange(startOffset, endOffset);
          resolvedSymbols.Declarations[startOffset] = new FSharpResolvedSymbolUse(symbolUse, textRange);
          resolvedSymbols.Uses.Remove(startOffset);
        }
        else
        {
          // workaround for indexer properties, visualfsharp#3933
          if (startOffset == endOffset ||
              mfv != null && mfv.IsProperty && buffer[endOffset - 1] == ']')
            continue;

          var nameRange = FixRange(startOffset, endOffset, mfv?.LogicalName, buffer, fsFile);

          // workaround for implicit type usages (e.g. in members with optional params), visualfsharp#3933
          if (symbol is FSharpEntity &&
              !(fsFile.FindTokenAt(new TreeOffset(nameRange.EndOffset - 1)) is FSharpIdentifierToken))
            continue;

          if (!resolvedSymbols.Declarations.ContainsKey(startOffset))
            resolvedSymbols.Uses[nameRange.StartOffset] = new FSharpResolvedSymbolUse(symbolUse, nameRange);
        }

        interruptChecker.CheckForInterrupt();
      }

      return resolvedSymbols;
    }

    private TextRange FixRange(int startOffset, int endOffset, [CanBeNull] string logicalName, IBuffer buffer,
      IFSharpFile fsFile)
    {
      // todo: remove when visualfsharp#3920 is implemented

      // trim foo.``bar`` to ``bar``
      const int minimumEscapedNameLength = 5;
      if (endOffset >= minimumEscapedNameLength && buffer.Length >= minimumEscapedNameLength &&
          buffer[endOffset - 1] == '`' && buffer[endOffset - 2] == '`')
        for (var i = endOffset - 4; i >= startOffset; i--)
          if (buffer[i] == '`' && buffer[i + 1] == '`')
            return new TextRange(i, endOffset);

      if (logicalName != null && PrettyNaming.IsMangledOpName(logicalName))
      {
        var sourceName = PrettyNaming.DecompileOpName.Invoke(logicalName);
        if (sourceName.Length == endOffset - startOffset)
          return new TextRange(startOffset, endOffset);

        // todo: use lexer buffer
        var token = fsFile.FindTokenAt(new TreeOffset(endOffset - 1));
        if (token != null)
        {
          var opText = token.GetTokenType() == FSharpTokenType.SYMBOLIC_OP ? sourceName : logicalName;
          return new TextRange(endOffset - opText.Length, endOffset);
        }
      }

      // trim foo.bar to bar
      for (var i = endOffset - 1; i > startOffset; i--)
        if (buffer[i].Equals('.'))
          return new TextRange(i + 1, endOffset);
      return new TextRange(startOffset, endOffset);
    }

    public void Invalidate()
    {
      for (var i = 0; i < myFileResolvedSymbols.Length; i++)
        myFileResolvedSymbols[i] = null;
    }
  }

  public class FSharpFileResolvedSymbols
  {
    public FSharpFileResolvedSymbols(int symbolUsesCount)
    {
      Declarations = new CompactMap<int, FSharpResolvedSymbolUse>(symbolUsesCount / 4);
      Uses = new CompactMap<int, FSharpResolvedSymbolUse>(symbolUsesCount);
    }

    [NotNull] public CompactMap<int, FSharpResolvedSymbolUse> Declarations;
    [NotNull] public CompactMap<int, FSharpResolvedSymbolUse> Uses;
  }
}
