using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.VisualStudio.FSharp.LanguageService;

namespace JetBrains.ReSharper.Feature.Services.FSharp.CodeCompletion
{
  [IntellisensePart]
  public class FSharpCodeCompletionContextProvider : CodeCompletionContextProviderBase
  {
    public override bool IsApplicable(CodeCompletionContext context)
    {
      return context.File is IFSharpFile;
    }

    public override ISpecificCodeCompletionContext GetCompletionContext(CodeCompletionContext context)
    {
      var file = context.File;
      var caretTreeOffset = context.CaretTreeOffset;
      var caretOffset = caretTreeOffset.Offset;

      var token = file.FindTokenAt(caretTreeOffset);
      var tokenBefore = file.FindTokenAt(caretTreeOffset - 1);
      var tokenBeforeType = tokenBefore?.GetTokenType();

      if (tokenBeforeType == FSharpTokenType.LINE_COMMENT ||
          tokenBeforeType == FSharpTokenType.DEAD_CODE || token?.GetTokenType() == FSharpTokenType.DEAD_CODE ||
          token == tokenBefore && tokenBeforeType != null &&
          (tokenBeforeType.IsComment || tokenBeforeType.IsStringLiteral ||
           tokenBeforeType.IsConstantLiteral) ||
          context.SelectedRange.TextRange.Length > 0 ||
          tokenBefore.GetTreeEndOffset() < caretTreeOffset || token.GetTreeEndOffset() < caretTreeOffset)
      {
        var selectedRange = context.SelectedRange.TextRange;
        return new FSharpCodeCompletionContext(context, new TextLookupRanges(selectedRange, selectedRange),
          TreeOffset.InvalidOffset, DocumentCoords.Empty, null, null, null, null, false);
      }

      var completedRangeStartOffset =
        tokenBeforeType != null && (tokenBeforeType == FSharpTokenType.IDENTIFIER || tokenBeforeType.IsKeyword)
          ? tokenBefore.GetTreeStartOffset().Offset
          : caretOffset;
      var completedRange = new TextRange(completedRangeStartOffset, caretOffset);
      var defaultRanges = GetTextLookupRanges(context, completedRange);

      var ranges = ShouldReplace(token)
        ? defaultRanges.WithReplaceRange(new TextRange(caretOffset,
          Math.Max(token.GetTreeEndOffset().Offset, caretOffset)))
        : defaultRanges;

      var document = context.Document;
      var coords = document.GetCoordsByOffset(caretOffset);
      var lineText = document.GetLineText(coords.Line);
      var names = QuickParse.GetPartialLongNameEx(lineText, (int) coords.Column - 1);

      return new FSharpCodeCompletionContext(context, ranges, caretTreeOffset, coords, names, tokenBefore, token,
        lineText);
    }

    private static bool ShouldReplace([CanBeNull] ITreeNode token)
    {
      var tokenType = token?.GetTokenType();
      return tokenType != null && (tokenType == FSharpTokenType.IDENTIFIER || tokenType.IsKeyword);
    }
  }
}