using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Threading;
using Microsoft.FSharp.Control;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public static class FSharpAsyncUtil
  {
    private const int InterruptCheckTimeout = 30;

    private static readonly Action ourDefaultInterruptCheck =
      () => Interruption.Current.CheckAndThrow();

    private static readonly FSharpReadLockRequestsQueue myReadRequests = new();

    /// <summary>
    /// Try to execute <paramref name="action"/> using read lock on the current thread.
    /// If not possible, queue a request to a thread calling FCS, possibly after a change on the main thread.
    /// When processing the request, the relevant psi module or declared element may already be removed and invalid.
    /// </summary>
    public static void UsingReadLockInsideFcs(IShellLocks locks, Action action)
    {
      // Try to acquire read lock on the current thread.
      // It should be possible, unless there's a write lock request that prevents it.
      if (locks.TryExecuteWithReadLock(action))
        return;

      // Could not get a read lock. Queue a request to be processed by a thread calling FCS.
      var tcs = new TaskCompletionSource<Unit>();
      myReadRequests.Enqueue(() =>
      {
        try
        {
          action();
          tcs.SetResult(Unit.Instance);
        }
        catch (Exception e) when (!e.IsOperationCanceled())
        {
          tcs.SetException(e);
        }
      });

      tcs.Task.Wait();
    }

    public static void ProcessEnqueuedReadRequests()
    {
      Interruption.Current.CheckAndThrow();
      while (myReadRequests.TryDequeue(out var request))
      {
        try
        {
          request();
        }
        catch (Exception e) when (e.IsOperationCanceled())
        {
          myReadRequests.Enqueue(request);
          throw;
        }
        Interruption.Current.CheckAndThrow();
      }
    }

    [CanBeNull]
    public static TResult RunAsTask<TResult>([NotNull] this FSharpAsync<TResult> async,
      [CanBeNull] Action interruptChecker = null)
    {
      interruptChecker ??= ourDefaultInterruptCheck;

      using var lifetimeDefinition = new LifetimeDefinition();
      var cancellationToken = lifetimeDefinition.Lifetime.ToCancellationToken();
      var task = FSharpAsync.StartAsTask(async, null, cancellationToken);

      task.ContinueWith(_ => myReadRequests.WakeUp(), CancellationToken.None,
        TaskContinuationOptions.ExecuteSynchronously, SynchronousScheduler.Instance);

      if (Shell.Instance.GetComponent<IShellLocks>().IsReadAccessAllowed())
        ShellLifetimes.ReadActivityLifetime.OnTermination(() => myReadRequests.WakeUp());

      while (!task.IsCompleted)
      {
        var action = myReadRequests.ExtractOrBlock(InterruptCheckTimeout, task);
        action?.Invoke();

        if (task.IsCompleted)
          break;

        interruptChecker();
      }

      return task.Result;
    }
  }
}
