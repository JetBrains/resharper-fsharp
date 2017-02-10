using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Daemon.FSharp.Highlightings
{
  public abstract class ErrorOrWarningHighlightingBase : IHighlighting
  {
    private readonly string myMessage;
    private readonly DocumentRange myRange;

    protected ErrorOrWarningHighlightingBase([NotNull] string message, DocumentRange range)
    {
      myMessage = message;
      myRange = range;
    }

    public string ToolTip => myMessage;
    public string ErrorStripeToolTip => myMessage;

    public bool IsValid()
    {
      return myRange.IsValid();
    }

    public DocumentRange CalculateRange()
    {
      return myRange;
    }
  }
}