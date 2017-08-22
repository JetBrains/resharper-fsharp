using System;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
{
  public abstract class FSharpDaemonStageProcessBase : IDaemonStageProcess
  {
    private const int InterruptCheckTime = 100;
    protected readonly SeldomInterruptCheckerWithCheckTime SeldomInterruptChecker;

    protected FSharpDaemonStageProcessBase(IDaemonProcess daemonProcess)
    {
      DaemonProcess = daemonProcess;
      SeldomInterruptChecker = new SeldomInterruptCheckerWithCheckTime(InterruptCheckTime);
    }

    public IDaemonProcess DaemonProcess { get; }
    public abstract void Execute(Action<DaemonStageResult> committer);

    [NotNull]
    protected static HighlightingInfo CreateHighlighting([NotNull] ITreeNode token, string highlightingAttributeId)
    {
      var range = token.GetNavigationRange();
      var highlighting = new FSharpIdentifierHighlighting(highlightingAttributeId, range);
      return new HighlightingInfo(range, highlighting);
    }
  }
}