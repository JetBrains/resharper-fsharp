using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Util;
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
      var caretOffset = context.CaretTreeOffset.Offset;
      var solution = context.Solution;
      var textControl = context.TextControl;

      var tokenAtCaret = TextControlToPsi.GetSourceTokenAtCaret(solution, textControl);
      var tokenBeforeCaret = TextControlToPsi.GetSourceTokenBeforeCaret(solution, textControl) as ITokenNode;
      var tokenType = tokenBeforeCaret?.GetTokenType();

      if (tokenAtCaret == tokenBeforeCaret && tokenType != null &&
          (tokenType.IsComment || tokenType.IsStringLiteral || tokenType.IsConstantLiteral))
        return null;

      var completedStartRange =
        tokenType != null && (tokenType == FSharpTokenType.IDENTIFIER || tokenType.IsKeyword)
          ? tokenBeforeCaret.GetTreeStartOffset().Offset
          : caretOffset;
      var completedRange = new TextRange(completedStartRange, caretOffset);
      var defaultRanges = GetTextLookupRanges(context, completedRange);

      var ranges = ShouldReplace(tokenAtCaret)
        ? defaultRanges.WithReplaceRange(new TextRange(caretOffset, tokenAtCaret.GetTreeEndOffset().Offset))
        : defaultRanges;

      return new FSharpCodeCompletionContext(context, ranges);
    }

    private static bool ShouldReplace([CanBeNull] ITreeNode token)
    {
      var tokenType = token?.GetTokenType();
      return tokenType != null && (tokenType == FSharpTokenType.IDENTIFIER || tokenType.IsKeyword);
    }
  }
}