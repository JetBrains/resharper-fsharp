using System;
using FSharp.Compiler;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
{
  [IntellisensePart]
  public class FSharpCodeCompletionContextProvider : CodeCompletionContextProviderBase
  {
    public override bool IsApplicable(CodeCompletionContext context) => context.File is IFSharpFile;

    public override ISpecificCodeCompletionContext GetCompletionContext(CodeCompletionContext context)
    {
      if (context.SelectedRange.TextRange.Length > 0)
        return null;

      var fsFile = (IFSharpFile) context.File;
      var parseTree = fsFile.ParseResults?.Value.ParseTree?.Value;
      if (parseTree == null)
        return null;

      var caretTreeOffset = context.CaretDocumentOffset;
      var caretOffset = caretTreeOffset.Offset;

      var token = fsFile.FindTokenAt(caretTreeOffset);
      var tokenType = token?.GetTokenType();
      var tokenBefore = fsFile.FindTokenAt(caretTreeOffset - 1);
      var tokenBeforeType = tokenBefore?.GetTokenType();

      if (tokenType == FSharpTokenType.COLON_COLON || tokenBeforeType == FSharpTokenType.COLON_COLON)
        return null;

      if (tokenBeforeType == FSharpTokenType.LINE_COMMENT ||
          tokenBeforeType == FSharpTokenType.DEAD_CODE || tokenType == FSharpTokenType.DEAD_CODE ||
          tokenBeforeType != null && (token == tokenBefore || token == null) &&
          (tokenBeforeType.IsComment || tokenBeforeType.IsStringLiteral ||
           tokenBeforeType.IsConstantLiteral) ||
          tokenBefore.GetDocumentEndOffset() < caretTreeOffset || token.GetDocumentEndOffset() < caretTreeOffset)
        return null;

      var document = context.Document;
      var completedRangeStartOffset = CanBeIdentifierPart(tokenBeforeType)
          ? tokenBefore.GetDocumentStartOffset().Offset
          : caretOffset;
      var completedRange = new DocumentRange(document, new TextRange(completedRangeStartOffset, caretOffset));
      var defaultRanges = GetTextLookupRanges(context, completedRange);

      var ranges = CanBeIdentifierPart(token?.GetTokenType())
        ? defaultRanges.WithReplaceRange(new DocumentRange(document, new TextRange(completedRangeStartOffset,
          Math.Max(token.GetDocumentEndOffset().Offset, caretOffset))))
        : defaultRanges;

      var coords = document.GetCoordsByOffset(caretOffset);
      var lineText = document.GetLineText(coords.Line);
      var partialName = QuickParse.GetPartialLongNameEx(lineText, (int) coords.Column - 1);
      var fsCompletionContext = UntypedParseImpl.TryGetCompletionContext(coords.ToPos(), parseTree, lineText);

      return new FSharpCodeCompletionContext(context, fsCompletionContext, ranges, coords, partialName, tokenBefore,
        token, lineText);
    }

    private static bool CanBeIdentifierPart([CanBeNull] ITokenNodeType type) =>
      type != null && (type == FSharpTokenType.IDENTIFIER || type == FSharpTokenType.UNDERSCORE || type.IsKeyword);
  }
}
