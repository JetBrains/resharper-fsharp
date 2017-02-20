using System;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Daemon.FSharp.Stages
{
  public abstract class FSharpDaemonStageProcessBase : IDaemonStageProcess
  {
    protected readonly int InterruptCheckTime = 100;
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