using System;
using System.Threading;
using System.Threading.Tasks;
using FSharp.Compiler;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Threading;
using JetBrains.Util.Logging;
using Microsoft.FSharp.Control;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public static class FSharpAsyncUtil
  {
    private const int InterruptCheckTimeout = 30;

    private static readonly Action DefaultInterruptCheck =
      () => Interruption.Current.CheckAndThrow();

    private static readonly FSharpReadLockRequestsQueue ReadRequests = new();

    /// <summary>
    /// Try to execute <paramref name="action"/> using read lock on the current thread.
    /// If not possible, queue a request to a thread calling FCS, possibly after a change on the main thread.
    /// When processing the request, the relevant psi module or declared element may already be removed and invalid.
    /// </summary>
    public static void UsingReadLockInsideFcs(IShellLocks locks, Action action, Func<bool> upToDateCheck = null)
    {
      // Capture FCS cancellation token so we can propagate cancellation in cases like F#->C#->F#,
      // where the last FCS request is initiated from inside reading the C# metadata,
      // and the initial request may get cancelled
      var fcsToken = Cancellable.Token;
      using var _ = Interruption.Current.Add(new LifetimeInterruptionSource(fcsToken));

      // Try to acquire read lock on the current thread.
      // It should be possible, unless there's a write lock request that prevents it.

      try
      {
        if (locks.TryExecuteWithReadLock(action))
          return;
      }
      catch (Exception e) when (e.IsOperationCanceled())
      {
        // The FCS request has originated from a R# thread, but was cancelled. We don't want to requeue this request.
        if (locks.IsReadAccessAllowed())
          throw;
      }
      catch (Exception e)
      {
        Logger.LogException(e);
        throw;
      }

      // Could not finish task under a read lock. Queue a request to be processed by a thread calling FCS.
      while (true)
      {
        // To ensure FCS metadata consistency, we retry cancelled requests in this loop.
        // This may happen when R# read lock is cancelled, but the corresponding FCS request in not (yet).
        // We should check the FCS request cancellation separately via its cancellation token.
        if (Cancellable.Token.IsCancellationRequested)
          break;

        var tcs = new TaskCompletionSource<Unit>();
        ReadRequests.Enqueue(() =>
        {
          // Add interruption source from the captured token on the thread executing the task. 

          // ReSharper disable once VariableHidesOuterVariable
          using var _ = Interruption.Current.Add(new LifetimeInterruptionSource(fcsToken));
          try
          {
            if (upToDateCheck == null || !upToDateCheck())
              action();

            tcs.SetResult(Unit.Instance);
          }
          catch (Exception e) when (e.IsOperationCanceled())
          {
            tcs.SetCanceled();
            throw;
          }
          catch (Exception e)
          {
            tcs.SetException(e);
          }
        });

        try
        {
          tcs.Task.Wait();
          break;
        }
        catch (Exception e) when (e.IsOperationCanceled())
        {
        }
        catch (Exception e)
        {
          Logger.LogException(e);
        }
      }
    }

    public static void ProcessEnqueuedReadRequests()
    {
      Interruption.Current.CheckAndThrow();
      while (ReadRequests.TryDequeue(out var request))
      {
        try
        {
          request.Invoke();
        }
        catch (Exception e)
        {
          Interruption.Current.CheckAndThrow();

          Logger.LogException("Unexpected exception", e);
          throw;
        }
      }
    }

    [CanBeNull]
    public static TResult RunAsTask<TResult>([NotNull] this FSharpAsync<TResult> async,
      [CanBeNull] Action interruptChecker = null)
    {
      interruptChecker ??= DefaultInterruptCheck;

      using var lifetimeDefinition = new LifetimeDefinition();
      var cancellationToken = lifetimeDefinition.Lifetime.ToCancellationToken();
      var task = FSharpAsync.StartAsTask(async, null, cancellationToken);

      task.ContinueWith(_ => ReadRequests.WakeUp(), CancellationToken.None,
        TaskContinuationOptions.ExecuteSynchronously, SynchronousScheduler.Instance);

      if (Shell.Instance.GetComponent<IShellLocks>().IsReadAccessAllowed())
        ShellLifetimes.ReadActivityLifetime.TryOnTermination(() => ReadRequests.WakeUp());

      while (!task.IsCompleted)
      {
        interruptChecker();

        var action = ReadRequests.ExtractOrBlock(InterruptCheckTimeout, task);
        action?.Invoke();

        if (task.IsCompleted)
          break;
      }

      return task.Result;
    }
  }
}
