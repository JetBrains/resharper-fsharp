using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

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
      var tokenType = tokenBeforeCaret?.GetTokenType();

      if (tokenAtCaret == tokenBeforeCaret && tokenType != null &&
          (tokenType.IsComment || tokenType.IsStringLiteral || tokenType.IsConstantLiteral))
        return null;

      var completedRangeStartOffset =
        tokenType != null && (tokenType == FSharpTokenType.IDENTIFIER || tokenType.IsKeyword)
          ? tokenBeforeCaret.GetTreeStartOffset().Offset
          : caretOffset;
      var completedRange = new TextRange(completedRangeStartOffset, caretOffset);
      var defaultRanges = GetTextLookupRanges(context, completedRange);

      var ranges = ShouldReplace(tokenAtCaret)
        ? defaultRanges.WithReplaceRange(new TextRange(caretOffset, tokenAtCaret.GetTreeEndOffset().Offset))
        : defaultRanges;

      context.Parameters.InitialLookupFocusBehaviour = LookupFocusBehaviour.Soft;
      return new FSharpCodeCompletionContext(context, ranges);
    }

    private static bool ShouldReplace([CanBeNull] ITreeNode token)
    {
      var tokenType = token?.GetTokenType();
      return tokenType != null && (tokenType == FSharpTokenType.IDENTIFIER || tokenType.IsKeyword);
    }
  }
}