using System;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Daemon.FSharp.Stages
{
  public abstract class FSharpDaemonStageProcessBase : IDaemonStageProcess
  {
    protected FSharpDaemonStageProcessBase(IDaemonProcess daemonProcess)
    {
      DaemonProcess = daemonProcess;
    }

    public IDaemonProcess DaemonProcess { get; }
    public abstract void Execute(Action<DaemonStageResult> committer);
  }
}