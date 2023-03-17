using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.Threading;
using JetBrains.Util.Logging;
using Microsoft.FSharp.Control;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public static class FSharpAsyncUtil
  {
    private const int InterruptCheckTimeout = 30;

    private static readonly Action ourDefaultInterruptCheck =
      () => Interruption.Current.CheckAndThrow();

    private static readonly ConcurrentQueue<Task> myReadRequests = new();

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
      var finished = false;
      while (!finished)
      {
        var task = new Task(action);
        myReadRequests.Enqueue(task);

        try
        {
          // Don't return the control until the request is processed.
          task.Wait();
          finished = true;
        }
        catch (Exception e) when (e.IsOperationCanceled())
        {
        }
        catch (Exception e)
        {
          Logger.LogException(e);
          throw;
        }
      }
    }

    public static void ProcessEnqueuedReadRequests()
    {
      while (myReadRequests.TryDequeue(out var request))
        request.RunSynchronously();
    }

    [CanBeNull]
    public static TResult RunAsTask<TResult>([NotNull] this FSharpAsync<TResult> async,
      [CanBeNull] Action interruptChecker = null)
    {
      interruptChecker ??= ourDefaultInterruptCheck;

      using var lifetimeDefinition = new LifetimeDefinition();
      var cancellationToken = lifetimeDefinition.Lifetime.ToCancellationToken();
      var task = FSharpAsync.StartAsTask(async, null, cancellationToken);

      while (!task.IsCompleted)
      {
        var finished = task.Wait(InterruptCheckTimeout, cancellationToken);
        if (finished) break;

        if (myReadRequests.TryDequeue(out var request))
          request.RunSynchronously();

        interruptChecker();
      }

      return task.Result;
    }
  }
}
