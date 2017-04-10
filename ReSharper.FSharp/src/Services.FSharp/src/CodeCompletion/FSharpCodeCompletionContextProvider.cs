using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
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

      var tokenBeforeCaret = file.FindTokenAt(caretTreeOffset - 1);
      var tokenAtCaret = file.FindTokenAt(caretTreeOffset);
      var tokenBeforeCaretType = tokenBeforeCaret?.GetTokenType();
      var tokenAtCaretType = tokenAtCaret?.GetTokenType();

      if (tokenBeforeCaretType == FSharpTokenType.LINE_COMMENT ||
          tokenBeforeCaretType == FSharpTokenType.DEAD_CODE || tokenAtCaretType == FSharpTokenType.DEAD_CODE)
        return null;

      if (tokenAtCaret == tokenBeforeCaret && tokenBeforeCaretType != null &&
          (tokenBeforeCaretType.IsComment || tokenBeforeCaretType.IsStringLiteral ||
           tokenBeforeCaretType.IsConstantLiteral))
        return null;

      var completedRangeStartOffset =
        tokenBeforeCaretType != null && (tokenBeforeCaretType == FSharpTokenType.IDENTIFIER ||
                                         tokenBeforeCaretType.IsKeyword)
          ? tokenBeforeCaret.GetTreeStartOffset().Offset
          : caretOffset;
      var completedRange = new TextRange(completedRangeStartOffset, caretOffset);
      var defaultRanges = GetTextLookupRanges(context, completedRange);

      var ranges = ShouldReplace(tokenAtCaret)
        ? defaultRanges.WithReplaceRange(new TextRange(caretOffset, tokenAtCaret.GetTreeEndOffset().Offset))
        : defaultRanges;

      var document = context.Document;
      var coords = document.GetCoordsByOffset(caretOffset);
      var names = QuickParse.GetPartialLongNameEx(document.GetLineText(coords.Line), (int) coords.Column - 1);

      return new FSharpCodeCompletionContext(context, ranges, caretTreeOffset, coords, names);
    }

    private static bool ShouldReplace([CanBeNull] ITreeNode token)
    {
      var tokenType = token?.GetTokenType();
      return tokenType != null && (tokenType == FSharpTokenType.IDENTIFIER || tokenType.IsKeyword);
    }
  }
}