using System;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Threading;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using PrettyNaming = Microsoft.FSharp.Compiler.PrettyNaming;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class FSharpModuleResolvedSymbols : IFSharpModuleResolvedSymbols
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

      PsiModule = psiModule;
      CheckerService = checkerService;
      ProjectOptionsProvider = projectOptionsProvider;
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

    [NotNull]
    private FSharpFileResolvedSymbols CreateFileResolvedSymbols(IPsiSourceFile sourceFile)
    {
      var interruptChecker = new SeldomInterruptCheckerWithCheckTime(100);
      // todo: cancellation
      if (!(sourceFile.GetPrimaryPsiFile() is IFSharpFile fsFile))
        return FSharpFileResolvedSymbols.Empty;

      var checkResults = CheckerService.ParseAndCheckFile(sourceFile)?.Value.CheckResults;
      var symbolUses = checkResults?.GetAllUsesOfAllSymbolsInFile().RunAsTask();
      if (symbolUses == null)
        return FSharpFileResolvedSymbols.Empty;

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
}
