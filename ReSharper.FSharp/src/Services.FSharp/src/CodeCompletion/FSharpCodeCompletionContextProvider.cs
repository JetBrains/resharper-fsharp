using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Cs.CodeCompletion
{
  [IntellisensePart]
  public class FSharpCodeCompletionContextProvider : CodeCompletionContextProviderBase
  {
    public override bool IsApplicable(CodeCompletionContext context) => context.File is IFSharpFile;

    public override ISpecificCodeCompletionContext GetCompletionContext(CodeCompletionContext context)
    {
      var fsFile = (IFSharpFile) context.File;
      var parseResults = fsFile.ParseResults;

      var caretTreeOffset = context.CaretTreeOffset;
      var caretOffset = caretTreeOffset.Offset;

      var token = fsFile.FindTokenAt(caretTreeOffset);
      var tokenBefore = fsFile.FindTokenAt(caretTreeOffset - 1);
      var tokenBeforeType = tokenBefore?.GetTokenType();

      var parseTree = parseResults?.Value.ParseTree?.Value;
      if (parseTree == null ||
          tokenBeforeType == FSharpTokenType.LINE_COMMENT ||
          tokenBeforeType == FSharpTokenType.DEAD_CODE || token?.GetTokenType() == FSharpTokenType.DEAD_CODE ||
          token == tokenBefore && tokenBeforeType != null &&
          (tokenBeforeType.IsComment || tokenBeforeType.IsStringLiteral ||
           tokenBeforeType.IsConstantLiteral) ||
          context.SelectedRange.TextRange.Length > 0 ||
          tokenBefore.GetTreeEndOffset() < caretTreeOffset || token.GetTreeEndOffset() < caretTreeOffset)
      {
        var selectedRange = context.SelectedRange;
        return new FSharpCodeCompletionContext(context, CompletionContext.Invalid,
          new TextLookupRanges(selectedRange, selectedRange), DocumentCoords.Empty, null);
      }

      var document = context.Document;
      var completedRangeStartOffset =
        tokenBeforeType != null && (tokenBeforeType == FSharpTokenType.IDENTIFIER || tokenBeforeType.IsKeyword)
          ? tokenBefore.GetTreeStartOffset().Offset
          : caretOffset;
      var completedRange = new DocumentRange(document, new TextRange(completedRangeStartOffset, caretOffset));
      var defaultRanges = GetTextLookupRanges(context, completedRange);

      var ranges = ShouldReplace(token)
        ? defaultRanges.WithReplaceRange(new DocumentRange(document, new TextRange(completedRangeStartOffset,
          Math.Max(token.GetTreeEndOffset().Offset, caretOffset))))
        : defaultRanges;

      var coords = document.GetCoordsByOffset(caretOffset);
      var lineText = document.GetLineText(coords.Line);
      var partialName = QuickParse.GetPartialLongNameEx(lineText, (int) coords.Column - 1);
      var fsCompletionContext = UntypedParseImpl.TryGetCompletionContext(coords.GetPos(), parseTree, lineText);

      return new FSharpCodeCompletionContext(context, fsCompletionContext, ranges, coords, partialName, tokenBefore,
        token, lineText);
    }

    private static bool ShouldReplace([CanBeNull] ITreeNode token)
    {
      var tokenType = token?.GetTokenType();
      return tokenType != null && (tokenType == FSharpTokenType.IDENTIFIER || tokenType.IsKeyword);
    }
  }
}