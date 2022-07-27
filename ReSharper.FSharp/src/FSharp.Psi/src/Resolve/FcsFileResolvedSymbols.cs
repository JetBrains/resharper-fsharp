using System;
using System.Collections.Generic;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using FSharp.Compiler.Text;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.TypingAssist;
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
using PrettyNaming = FSharp.Compiler.Syntax.PrettyNaming;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class FcsFileResolvedSymbols : IFcsFileResolvedSymbols
  {
    private const string OpName = "FSharpFileResolvedSymbols";

    private ResolvedSymbols mySymbols;
    private readonly object myLock = new object();

    [NotNull] public IPsiSourceFile SourceFile { get; }

    public FcsFileResolvedSymbols([NotNull] IPsiSourceFile sourceFile)
    {
      SourceFile = sourceFile;
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

    public IReadOnlyList<FcsResolvedSymbolUse> GetAllDeclaredSymbols()
    {
      var resolvedSymbols = GetResolvedSymbols();
      return resolvedSymbols.Declarations.Values.AsChunkIReadOnlyList();
    }

    public IReadOnlyList<FcsResolvedSymbolUse> GetAllResolvedSymbols()
    {
      var resolvedSymbols = GetResolvedSymbols();
      return resolvedSymbols.Uses.Values.AsChunkIReadOnlyList();
    }

    public FSharpSymbol GetSymbol(int offset) =>
      GetSymbolDeclaration(offset)?.Symbol ?? GetSymbolUse(offset)?.Symbol;

    [NotNull]
    private ResolvedSymbols CreateFileResolvedSymbols()
    {
      // todo: cancellation
      if (!(SourceFile.GetPrimaryPsiFile() is IFSharpFile fsFile))
        return ResolvedSymbols.Empty;

      var checkResults = fsFile.GetParseAndCheckResults(false, OpName)?.Value.CheckResults;
      var symbolUses = checkResults?.GetAllUsesOfAllSymbolsInFile(null);
      if (symbolUses == null)
        return ResolvedSymbols.Empty;

      var document = SourceFile.Document;
      var lexer = fsFile.CachingLexer;
      var buffer = document.Buffer;
      var resolvedSymbols = new ResolvedSymbols(symbolUses.Length);
      foreach (var symbolUse in symbolUses)
      {
        var symbol = symbolUse.Symbol;
        var range = symbolUse.Range;

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
            if (RangeModule.equals(activePatternCase.DeclarationLocation, range))
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
              resolvedSymbols.Declarations[caseStartOffset] = new FcsResolvedSymbolUse(symbolUse, caseTextRange);
              continue;
            }

            var caseUseInBindingRange = new TextRange(startOffset, endOffset);
            resolvedSymbols.Uses[startOffset] = new FcsResolvedSymbolUse(symbolUse, caseUseInBindingRange);
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

          resolvedSymbols.Declarations[startOffset] = new FcsResolvedSymbolUse(symbolUse, textRange);
          resolvedSymbols.Uses.Remove(startOffset);
        }
        else
        {
          // workaround for indexer properties, visualfsharp#3933
          if (startOffset == endOffset || mfv is { IsProperty: true } && buffer[endOffset - 1] == ']')
            continue;

          var entity =
            symbol as FSharpEntity ?? (mfv is { IsConstructor: true } ? mfv.DeclaringEntity?.Value : null);

          // we need `foo` in
          // inherit mod.foo<bar.baz>()
          if (entity != null)
          {
            var isStaticInstantiation = entity.IsStaticInstantiation;
            if (!entity.GenericParameters.IsEmpty() || isStaticInstantiation)
            {
              if (lexer.FindTokenAt(endOffset - 1) && lexer.TokenType == FSharpTokenType.GREATER)
              {
                if (new ParenMatcher().FindMatchingBracket(lexer) && lexer.TokenStart >= startOffset)
                {
                  lexer.Advance(-1);
                  if (lexer.TokenType != null)
                  {
                    if (isStaticInstantiation && resolvedSymbols.Uses.ContainsKey(startOffset))
                      continue;

                    startOffset = lexer.TokenStart;
                    endOffset = lexer.TokenEnd;
                  }
                }
              }
            }
          }

          var mfvLogicalName = mfv?.LogicalName;
          var nameRange = FixRange(startOffset, endOffset, mfvLogicalName, buffer, lexer);
          startOffset = nameRange.StartOffset;

          var isCtor = mfv is { IsConstructor: true };

          // workaround for implicit type usages (e.g. in members with optional params), visualfsharp#3933
          if ((CanIgnoreSymbol(symbol, isCtor) || CanIgnoreMfv(mfvLogicalName)) &&
              !(lexer.FindTokenAt(nameRange.EndOffset - 1) && (lexer.TokenType?.IsIdentifier ?? false)))
            continue;

          if (mfvLogicalName == "GetReverseIndex" && lexer.TokenType == FSharpTokenType.SYMBOLIC_OP)
            continue;

          // IsFromPattern helps in cases where fake value is created at range,
          // e.g. `fun Literal -> ()` has both pattern and binding symbols at pattern range.
          if (symbolUse.IsFromPattern || !resolvedSymbols.Declarations.ContainsKey(startOffset))
          {
            if (resolvedSymbols.Uses.TryGetValue(startOffset, out var existingSymbol) &&
                existingSymbol.SymbolUse.Symbol is FSharpEntity && !isCtor)
              continue;

            resolvedSymbols.Uses[startOffset] = new FcsResolvedSymbolUse(symbolUse, nameRange);
          }

          if (symbolUse.IsFromPattern)
            resolvedSymbols.Declarations.Remove(startOffset);
        }

        Interruption.Current.CheckAndThrow();
      }

      return resolvedSymbols;
    }

    private static bool CanIgnoreSymbol([NotNull] FSharpSymbol symbol, bool isCtor) =>
      isCtor || symbol is FSharpEntity;

    private static bool CanIgnoreMfv([CanBeNull] string n) =>
      n == "op_Range" || n == "op_RangeStep" || n == "GetReverseIndex" || n == "GetSlice";

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
        var sourceName = PrettyNaming.DecompileOpName(logicalName);
        var isUnary = sourceName.StartsWith("~", StringComparison.Ordinal);
        var sourceLength = isUnary ? sourceName.Length - 1 : sourceName.Length;

        if (sourceLength == endOffset - startOffset)
          return new TextRange(startOffset, endOffset);

        // todo: use lexer buffer
        if (lexer.FindTokenAt(endOffset - 1) && lexer.TokenType is { } tokenType)
        {
          if (tokenType == FSharpTokenType.LPAREN_STAR_RPAREN && sourceName == "*")
            return new TextRange(endOffset - 3, endOffset);

          var opText = tokenType == FSharpTokenType.SYMBOLIC_OP ? sourceName : logicalName;
          return new TextRange(endOffset - opText.Length, endOffset);
        }
      }

      // We need symbol use start offset to be synchronized with FCS; trim qualifiers:
      // `foo.bar` -> `bar`
      // `foo.(+) -> +
      // `foo.(|A|) -> (|A|)
      // todo: align operators and active patterns handling in the parser
      for (var i = endOffset - 1; i > startOffset; i--)
      {
        var c = buffer[i];
        if (c.Equals('.') || c.Equals('(') && i < endOffset - 1 && buffer[i + 1] != '|')
          return new TextRange(i + 1, endOffset);
      }

      return new TextRange(startOffset, endOffset);
    }

    private class ResolvedSymbols
    {
      public static readonly ResolvedSymbols Empty = new ResolvedSymbols();

      [NotNull] internal readonly CompactMap<int, FcsResolvedSymbolUse> Declarations;
      [NotNull] internal readonly CompactMap<int, FcsResolvedSymbolUse> Uses;

      public ResolvedSymbols(int symbolUsesCount = 0)
      {
        Declarations = new CompactMap<int, FcsResolvedSymbolUse>(symbolUsesCount / 4);
        Uses = new CompactMap<int, FcsResolvedSymbolUse>(symbolUsesCount);
      }
    }

    private class ParenMatcher : BracketMatcher
    {
      private static readonly Pair<TokenNodeType, TokenNodeType>[] Parens =
        {new Pair<TokenNodeType, TokenNodeType>(FSharpTokenType.LESS, FSharpTokenType.GREATER)};

      public ParenMatcher() : base(Parens)
      {
      }
    }
  }
}
