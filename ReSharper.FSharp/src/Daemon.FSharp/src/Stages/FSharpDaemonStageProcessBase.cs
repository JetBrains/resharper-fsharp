using System;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
{
  [AllowNullLiteral]
  public abstract class FSharpDaemonStageProcessBase : IDaemonStageProcess
  {
    private const int InterruptCheckTime = 20;
    protected readonly SeldomInterruptCheckerWithCheckTime SeldomInterruptChecker;

    protected FSharpDaemonStageProcessBase(IDaemonProcess daemonProcess)
    {
      DaemonProcess = daemonProcess;
      SeldomInterruptChecker = new SeldomInterruptCheckerWithCheckTime(InterruptCheckTime);
    }

    public IDaemonProcess DaemonProcess { get; }
    public abstract void Execute(Action<DaemonStageResult> committer);
  }
}