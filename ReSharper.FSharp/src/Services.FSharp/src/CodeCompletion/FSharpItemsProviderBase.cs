using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;

namespace JetBrains.ReSharper.Feature.Services.FSharp.CodeCompletion
{
  public abstract class FSharpItemsProviderBase : ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(FSharpCodeCompletionContext context)
    {
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