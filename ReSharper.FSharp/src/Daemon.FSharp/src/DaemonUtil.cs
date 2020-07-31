using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs
{
  public static class DaemonUtil
  {
    public static Action CreateInterruptChecker([NotNull] this IDaemonProcess process)
    {
      return () =>
      {
        if (process.InterruptFlag) throw new OperationCanceledException();
      };
    }
  }
}