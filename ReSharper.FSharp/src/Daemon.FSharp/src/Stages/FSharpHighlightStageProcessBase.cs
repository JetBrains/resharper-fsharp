using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.FSharp.Highlightings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Daemon.FSharp.Stages
{
  public abstract class FSharpHighlightStageProcessBase : FSharpDaemonStageProcessBase
  {
    [NotNull] protected readonly IFSharpFile FsFile;

    protected FSharpHighlightStageProcessBase([NotNull] IFSharpFile fsFile, [NotNull] IDaemonProcess daemonProcess)
      : base(daemonProcess)
    {
      FsFile = fsFile;
    }

    protected static HighlightingInfo CreateHighlighting(ITreeNode token, string highlightingAttributeId)
    {
      var range = token.GetNavigationRange();
      var highlighting = new FSharpIdentifierHighlighting(highlightingAttributeId, range);
      return new HighlightingInfo(range, highlighting);
    }
  }
}