using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.Threading;
using Microsoft.FSharp.Control;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public static class FSharpAsyncUtil
  {
    private const int InterruptCheckTimeout = 30;

    private static readonly Action ourDefaultInterruptCheck =
      () => Interruption.Current.CheckAndThrow();

    private static readonly ConcurrentQueue<Task> myReadRequests = new();

    public static void UsingReadLockInsideFcs(IShellLocks locks, Action action)
    {
      // Try to acquire read lock on the current thread.
      // It should be possible, unless there's a write lock request that prevents it.
      if (locks.TryExecuteWithReadLock(action))
        return;

      // Could not get a read lock. Queue a request to for threads waiting for FCS to process. 
      var task = new Task(action);
      myReadRequests.Enqueue(task);

      // Don't return the control until the request is processed.
      task.Wait();
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
