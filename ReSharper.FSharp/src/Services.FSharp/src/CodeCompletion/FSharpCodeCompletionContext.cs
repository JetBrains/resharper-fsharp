using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;

namespace JetBrains.ReSharper.Feature.Services.FSharp.CodeCompletion
{
  public class FSharpCodeCompletionContext : SpecificCodeCompletionContext
  {
    public FSharpCodeCompletionContext([NotNull] CodeCompletionContext context, TextLookupRanges ranges) : base(context)
    {
      Ranges = ranges;
    }

    public override string ContextId => "FSharpCodeCompletionContext";
    public TextLookupRanges Ranges { get; }
  }
}