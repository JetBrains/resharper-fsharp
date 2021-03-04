using System;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Threading;
using Microsoft.FSharp.Control;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public static class FSharpAsyncUtil
  {
    private const int InterruptCheckTimeout = 30;
    private const int GetTransferredWriteLockTimeout = 10;

    private static readonly CancellationToken ourEternalCancellationToken =
      new CancellationTokenSource().Token;

    private static readonly Action ourDefaultInterruptCheck =
      () => InterruptableActivityCookie.CheckAndThrow();

    [CanBeNull]
    public static T RunAsTask<T>([NotNull] this FSharpAsync<T> async, [CanBeNull] Action interruptChecker = null) =>
      Shell.Instance.GetComponent<IShellLocks>().IsWriteAccessAllowed()
        ? RunTransferringWriteLock(async)
        : RunInterrupting(async, interruptChecker);

    private static TResult RunInterrupting<TResult>([NotNull] FSharpAsync<TResult> async,
      [CanBeNull] Action interruptChecker)
    {
      interruptChecker ??= ourDefaultInterruptCheck;

      var cancellationTokenSource = new CancellationTokenSource();
      var cancellationToken = cancellationTokenSource.Token;
      var task = FSharpAsync.StartAsTask(async, null, cancellationToken);

      while (!task.IsCompleted)
      {
        var finished = task.Wait(InterruptCheckTimeout, cancellationToken);
        if (finished) break;
        try
        {
          interruptChecker();
        }
        catch (Exception e) when (e.IsOperationCanceled())
        {
          cancellationTokenSource.Cancel();
          throw;
        }
      }

      return task.Result;
    }

    /// <summary>
    /// Prevents dead locks when waiting for FCS while under write lock.
    /// FCS may request a read lock from a background thread before processing this request.
    /// We grant read access by passing the write lock.
    /// </summary>
    private static T RunTransferringWriteLock<T>(FSharpAsync<T> async)
    {
      var shellLocks = Shell.Instance.GetComponent<IShellLocks>();
      shellLocks.AssertWriteAccessAllowed();

      var task = FSharpAsync.StartAsTask(async, null, null);
      var isLockTransferred = false;

      while (!task.IsCompleted || isLockTransferred)
      {
        var finished = task.Wait(InterruptCheckTimeout, ourEternalCancellationToken);
        if (finished)
        {
          if (!isLockTransferred)
            break;

          Thread.Sleep(GetTransferredWriteLockTimeout);
        }

        if (!isLockTransferred && FSharpLocks.ThreadRequestingWriteLock != null)
        {
          isLockTransferred = true;
          shellLocks.ContentModelLocks.TransferWriteLock(FSharpLocks.ThreadRequestingWriteLock);
        }

        if (isLockTransferred && shellLocks.IsWriteAccessAllowed())
          isLockTransferred = false;
      }

      return task.Result;
    }
  }
}
