using System;
using System.Collections.Generic;
using FSharp.Compiler;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.TypingAssist;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using PrettyNaming = FSharp.Compiler.PrettyNaming;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class FSharpFileResolvedSymbols : IFSharpFileResolvedSymbols
  {
    private const string OpName = "FSharpFileResolvedSymbols";

    private ResolvedSymbols mySymbols;
    private readonly object myLock = new object();

    [NotNull] public IPsiSourceFile SourceFile { get; }
    [NotNull] public FSharpCheckerService CheckerService { get; }

    public FSharpFileResolvedSymbols([NotNull] IPsiSourceFile sourceFile, [NotNull] FSharpCheckerService checkerService)
    {
      SourceFile = sourceFile;
      CheckerService = checkerService;
    }

    private ResolvedSymbols GetResolvedSymbols()
    {
      lock (myLock)
      {
        if (mySymbols != null)
          return mySymbols;

        mySymbols = CreateFileResolvedSymbols();
        return mySymbols;
      }
    }

    public FSharpSymbolUse GetSymbolUse(int offset)
    {
      var resolvedSymbols = GetResolvedSymbols();

      var resolvedSymbol = resolvedSymbols.Uses.TryGetValue(offset);
      if (resolvedSymbol == null)
        return null;

      return resolvedSymbols.Declarations.TryGetValue(offset) == null
        ? resolvedSymbol.SymbolUse
        : null;
    }

    public FSharpSymbolUse GetSymbolDeclaration(int offset)
    {
      var resolvedSymbols = GetResolvedSymbols();
      return resolvedSymbols.Declarations.TryGetValue(offset)?.SymbolUse;
    }

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllDeclaredSymbols()
    {
      var resolvedSymbols = GetResolvedSymbols();
      return resolvedSymbols.Declarations.Values.AsChunkIReadOnlyList();
    }

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllResolvedSymbols()
    {
      var resolvedSymbols = GetResolvedSymbols();
      return resolvedSymbols.Uses.Values.AsChunkIReadOnlyList();
    }

    public FSharpSymbol GetSymbol(int offset) =>
      GetSymbolDeclaration(offset)?.Symbol ?? GetSymbolUse(offset)?.Symbol;

    [NotNull]
    private ResolvedSymbols CreateFileResolvedSymbols()
    {
      var interruptChecker = new SeldomInterruptCheckerWithCheckTime(100);
      // todo: cancellation
      if (!(SourceFile.GetPrimaryPsiFile() is IFSharpFile fsFile))
        return ResolvedSymbols.Empty;

      var checkResults = CheckerService.ParseAndCheckFile(SourceFile, OpName)?.Value.CheckResults;
      var symbolUses = checkResults?.GetAllUsesOfAllSymbolsInFile().RunAsTask();
      if (symbolUses == null)
        return ResolvedSymbols.Empty;

      var document = SourceFile.Document;
      var lexer = fsFile.CachingLexer;
      var buffer = document.Buffer;
      var resolvedSymbols = new ResolvedSymbols(symbolUses.Length);
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
                !(lexer.FindTokenAt(endOffset - 1) && (lexer.TokenType?.IsIdentifier ?? false) || mfv.IsActivePattern))
              continue;

            if (mfvLogicalName == "Invoke" && (mfv.DeclaringEntity?.Value?.IsDelegate ?? false))
              continue;

            var len = endOffset - startOffset;
            if (mfvLogicalName == "op_Multiply" && len == 3)
            {
              // The `*` pattern includes parens and is parsed as special token
              // let (*) (_, _) = ()
              startOffset++;
              endOffset--;
            }
          }
          else if (activePatternCase != null)
          {
            // Skip active pattern cases bindings as these have incorrect ranges.
            // Active pattern cases uses inside bindings are currently marked as bindings so check the range.
            // https://github.com/Microsoft/visualfsharp/issues/4423
            if (Range.equals(activePatternCase.DeclarationLocation, range))
            {
              var activePatternId = fsFile.GetContainingNodeAt<ActivePatternId>(new TreeOffset(endOffset - 1));
              if (activePatternId == null)
                continue;

              var cases = activePatternId.Cases;
              var caseIndex = activePatternCase.Index;
              if (caseIndex < 0 || caseIndex >= cases.Count)
                continue;

              if (!(cases[caseIndex] is IActivePatternNamedCaseDeclaration caseDeclaration))
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
            if (!(lexer.FindTokenAt(endOffset - 1) && (lexer.TokenType?.IsIdentifier ?? false)))
              continue;
          }

          var textRange = mfv != null
            ? new TextRange(startOffset, endOffset)
            : FixRange(startOffset, endOffset, null, buffer, lexer);
          startOffset = textRange.StartOffset;

          resolvedSymbols.Declarations[startOffset] = new FSharpResolvedSymbolUse(symbolUse, textRange);
          resolvedSymbols.Uses.Remove(startOffset);
        }
        else
        {
          // workaround for indexer properties, visualfsharp#3933
          if (startOffset == endOffset ||
              mfv != null && mfv.IsProperty && buffer[endOffset - 1] == ']')
            continue;

          var entity =
            symbol as FSharpEntity ??
            (mfv != null && mfv.IsConstructor ? mfv.DeclaringEntity?.Value : null);

          // we need `foo` in
          // inherit mod.foo<bar.baz>()
          if (entity != null && !entity.GenericParameters.IsEmpty())
          {
            if (lexer.FindTokenAt(endOffset - 1) && lexer.TokenType == FSharpTokenType.GREATER)
            {
              if (new ParenMatcher().FindMatchingBracket(lexer) && lexer.TokenStart >= startOffset)
              {
                lexer.Advance(-1);
                if (lexer.TokenType != null)
                {
                  startOffset = lexer.TokenStart;
                  endOffset = lexer.TokenEnd;
                }
              }
            }
          }

          var nameRange = FixRange(startOffset, endOffset, mfv?.LogicalName, buffer, lexer);
          startOffset = nameRange.StartOffset;

          // workaround for implicit type usages (e.g. in members with optional params), visualfsharp#3933
          if (CanIgnoreSymbol(symbol) &&
              !(lexer.FindTokenAt(nameRange.EndOffset - 1) && (lexer.TokenType?.IsIdentifier ?? false)))
            continue;

          // IsFromPattern helps in cases where fake value is created at range,
          // e.g. `fun Literal -> ()` has both pattern and binding symbols at pattern range. 
          if (symbolUse.IsFromPattern || !resolvedSymbols.Declarations.ContainsKey(startOffset))
            resolvedSymbols.Uses[startOffset] = new FSharpResolvedSymbolUse(symbolUse, nameRange);
          if (symbolUse.IsFromPattern)
            resolvedSymbols.Declarations.Remove(startOffset);
        }

        interruptChecker.CheckForInterrupt();
      }

      return resolvedSymbols;
    }

    private static bool CanIgnoreSymbol([NotNull] FSharpSymbol symbol) =>
      symbol is FSharpEntity || 
      symbol is FSharpMemberOrFunctionOrValue mfv && mfv.LogicalName == "op_RangeStep";

    private TextRange FixRange(int startOffset, int endOffset, [CanBeNull] string logicalName, IBuffer buffer,
      CachingLexer lexer)
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
        var isUnary = sourceName.StartsWith("~", StringComparison.Ordinal);
        var sourceLength = isUnary ? sourceName.Length - 1 : sourceName.Length;

        if (sourceLength == endOffset - startOffset)
          return new TextRange(startOffset, endOffset);

        // todo: use lexer buffer
        if (lexer.FindTokenAt(endOffset - 1) && lexer.TokenType is TokenNodeType tokenType)
        {
          var opText = tokenType == FSharpTokenType.SYMBOLIC_OP ? sourceName : logicalName;
          return new TextRange(endOffset - opText.Length, endOffset);
        }
      }

      // trim foo.bar to bar
      for (var i = endOffset - 1; i > startOffset; i--)
        if (buffer[i].Equals('.'))
          return new TextRange(i + 1, endOffset);
      return new TextRange(startOffset, endOffset);
    }

    private class ResolvedSymbols
    {
      public static readonly ResolvedSymbols Empty = new ResolvedSymbols();

      [NotNull] internal readonly CompactMap<int, FSharpResolvedSymbolUse> Declarations;
      [NotNull] internal readonly CompactMap<int, FSharpResolvedSymbolUse> Uses;

      public ResolvedSymbols(int symbolUsesCount = 0)
      {
        Declarations = new CompactMap<int, FSharpResolvedSymbolUse>(symbolUsesCount / 4);
        Uses = new CompactMap<int, FSharpResolvedSymbolUse>(symbolUsesCount);
      }
    }

    private class ParenMatcher : BracketMatcher
    {
      private static readonly Pair<TokenNodeType, TokenNodeType>[] ourParens =
        {new Pair<TokenNodeType, TokenNodeType>(FSharpTokenType.LESS, FSharpTokenType.GREATER)};

      public ParenMatcher() : base(ourParens)
      {
      }
    }
  }
}
