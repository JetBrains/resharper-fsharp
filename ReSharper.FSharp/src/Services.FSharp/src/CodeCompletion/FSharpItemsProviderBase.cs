using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Cs.CodeCompletion
{
  public abstract class FSharpItemsProviderBase : ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(FSharpCodeCompletionContext context)
    {
      // todo: change when it's possible to disable smart completion on the second invokation
      // return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
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