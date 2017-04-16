using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Feature.Services.FSharp.CodeCompletion
{
  public abstract class FSharpItemsProviderBase : ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(FSharpCodeCompletionContext context)
    {
      var tokenType = context.TokenAtCaret?.GetTokenType();
      var tokenBeforeType = context.TokenBeforeCaret?.GetTokenType();
      if (tokenBeforeType == FSharpTokenType.LINE_COMMENT ||
          tokenBeforeType == FSharpTokenType.DEAD_CODE || tokenType == FSharpTokenType.DEAD_CODE)
        return false;

      if (context.TokenBeforeCaret == context.TokenAtCaret && tokenBeforeType != null &&
          (tokenBeforeType.IsComment || tokenBeforeType.IsStringLiteral || tokenBeforeType.IsConstantLiteral))
        return false;

//      return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
      // for some reason completion type becomes smart on subsequent invokations
      return true;
    }

    protected override TextLookupRanges GetDefaultRanges(FSharpCodeCompletionContext context)
    {
      return context.Ranges;
    }

    protected override LookupFocusBehaviour GetLookupFocusBehaviour(FSharpCodeCompletionContext context)
    {
      return LookupFocusBehaviour.Soft;
    }
  }
}