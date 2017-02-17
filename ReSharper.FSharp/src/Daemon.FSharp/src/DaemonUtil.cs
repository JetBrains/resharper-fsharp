using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Daemon.FSharp
{
  public static class DaemonUtil
  {
    public static Action CreateInterruptChecker([NotNull] this IDaemonProcess process)
    {
      return () =>
      {
        if (process.InterruptFlag) throw new ProcessCancelledException();
      };
    }
  }
}