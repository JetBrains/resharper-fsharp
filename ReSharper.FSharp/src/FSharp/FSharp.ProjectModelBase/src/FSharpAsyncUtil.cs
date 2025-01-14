using System;
using System.Threading;
using System.Threading.Tasks;
using FSharp.Compiler;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Diagnostics;
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
      var logger = Logger.GetLogger(typeof(FSharpAsyncUtil));

      // Capture FCS cancellation token so we can propagate cancellation in cases like F#->C#->F#,
      // where the last FCS request is initiated from inside reading the C# metadata,
      // and the initial request may get cancelled
      var fcsToken = Cancellable.HasCancellationToken ? Cancellable.Token : CancellationToken.None;
      using var _ = Interruption.Current.Add(new LifetimeInterruptionSource(fcsToken));

      // Try to acquire read lock on the current thread.
      // It should be possible, unless there's a write lock request that prevents it.

      try
      {
        if (locks.TryExecuteWithReadLock(action))
        {
          logger.Trace("UsingReadLockInsideFcs: executed on the initial thread, returning");
          return;
        }
      }
      catch (Exception e) when (e.IsOperationCanceled())
      {
        logger.Trace("UsingReadLockInsideFcs: exception: the operation cancelled on the initial thread");

        // The FCS request has originated from a R# thread, but was cancelled. We don't want to requeue this request.
        if (locks.IsReadAccessAllowed())
        {
          logger.Trace("UsingReadLockInsideFcs: read lock was acquired before the request, rethrowing");
          throw;
        }
      }
      catch (Exception e)
      {
        logger.Trace("UsingReadLockInsideFcs: exception: rethrowing");
        Logger.LogException(e);
        throw;
      }

      // Could not finish task under a read lock. Queue a request to be processed by a thread calling FCS.
      logger.Trace("UsingReadLockInsideFcs: before the loop");
      while (true)
      {
        // To ensure FCS metadata consistency, we retry cancelled requests in this loop.
        // This may happen when R# read lock is cancelled, but the corresponding FCS request in not (yet).
        // We should check the FCS request cancellation separately via its cancellation token.
        if (Cancellable.HasCancellationToken && Cancellable.Token.IsCancellationRequested)
        {
          logger.Trace("UsingReadLockInsideFcs: FCS token is cancelled");
          break;
        }

        logger.Trace("UsingReadLockInsideFcs: enqueueing request");
        var tcs = new TaskCompletionSource<Unit>();
        ReadRequests.Enqueue(() =>
        {
          logger.Trace("UsingReadLockInsideFcs: inside request lambda");
          // Add interruption source from the captured token on the thread executing the task. 

          // ReSharper disable once VariableHidesOuterVariable
          using var _ = Interruption.Current.Add(new LifetimeInterruptionSource(fcsToken));
          try
          {
            if (upToDateCheck == null || !upToDateCheck())
            {
              logger.Trace("UsingReadLockInsideFcs: before action");
              action();
              logger.Trace("UsingReadLockInsideFcs: after action");
            }
            else
            {
              logger.Trace("UsingReadLockInsideFcs: action not performed due to upToDateCheck");
            }

            logger.Trace("UsingReadLockInsideFcs: setting result");
            tcs.SetResult(Unit.Instance);
          }
          catch (Exception e) when (e.IsOperationCanceled())
          {
            logger.Trace("UsingReadLockInsideFcs: exception: cancelled, before tcs.SetCanceled");
            tcs.SetCanceled();
            logger.Trace("UsingReadLockInsideFcs: exception: after tcs.SetCanceled, rethrowing");
            throw;
          }
          catch (Exception e)
          {
            logger.Trace("UsingReadLockInsideFcs: exception: before tcs.SetException");
            tcs.SetException(e);
          }
        });
        logger.Trace("UsingReadLockInsideFcs: enqueued request");

        try
        {
          logger.Trace("UsingReadLockInsideFcs: before tcs.Task.Wait");
          tcs.Task.Wait();
          logger.Trace("UsingReadLockInsideFcs: after tcs.Task.Wait");
          break;
        }
        catch (Exception e) when (e.IsOperationCanceled())
        {
          logger.Trace("UsingReadLockInsideFcs: exception: cancelled");
        }
        catch (Exception e)
        {
          logger.Trace("UsingReadLockInsideFcs: exception");
          Logger.LogException(e);
        }
      }
    }

    public static void ProcessEnqueuedReadRequests()
    {
      var logger = Logger.GetLogger(typeof(FSharpAsyncUtil));

      logger.Trace("ProcessEnqueuedReadRequests: before outer CheckAndThrow");
      Interruption.Current.CheckAndThrow();

      logger.Trace("ProcessEnqueuedReadRequests: before loop");
      while (ReadRequests.TryDequeue(out var request))
      {
        try
        {
          logger.Trace("ProcessEnqueuedReadRequests: before request.Invoke");
          request.Invoke();
        }
        catch (Exception e)
        {
          logger.Trace("ProcessEnqueuedReadRequests: exception");
          Logger.LogException("Unexpected exception", e);

          logger.Trace("ProcessEnqueuedReadRequests: before inner CheckAndThrow");
          Interruption.Current.CheckAndThrow();

          logger.Trace("ProcessEnqueuedReadRequests: exception: rethrowing");
          throw;
        }
      }
      logger.Trace("ProcessEnqueuedReadRequests: after loop");
    }

    [CanBeNull]
    public static TResult RunAsTask<TResult>([NotNull] this FSharpAsync<TResult> async,
      [CanBeNull] Action interruptChecker = null)
    {
      var logger = Logger.GetLogger(typeof(FSharpAsyncUtil));

      interruptChecker ??= DefaultInterruptCheck;

      using var lifetimeDefinition = new LifetimeDefinition();
      var cancellationToken = lifetimeDefinition.Lifetime.ToCancellationToken();

      logger.Trace("RunAsTask: before StartAsTask");
      var task = FSharpAsync.StartAsTask(async, null, cancellationToken);
      logger.Trace("RunAsTask: after StartAsTask");

      task.ContinueWith(_ =>
        {
          logger.Trace("RunAsTask: Inside ContinueWith");
          ReadRequests.WakeUp();
        }, CancellationToken.None,
        TaskContinuationOptions.ExecuteSynchronously, SynchronousScheduler.Instance);

      if (Shell.Instance.GetComponent<IShellLocks>().IsReadAccessAllowed())
        ShellLifetimes.ReadActivityLifetime.TryOnTermination(() =>
        {
          logger.Trace("RunAsTask: Inside ReadActivityLifetime.TryOnTermination");
          ReadRequests.WakeUp();
        });

      logger.Trace("RunAsTask: before loop");
      while (!task.IsCompleted)
      {
        interruptChecker();

        var action = ReadRequests.ExtractOrBlock(InterruptCheckTimeout, task);
        if (action != null)
        {
          logger.Trace("RunAsTask: got metadata request, before Invoke");
          action();
          logger.Trace("RunAsTask: after Invoke");
        }

        if (task.IsCompleted)
          break;
      }
      logger.Trace("RunAsTask: after loop");

      return task.Result;
    }
  }
}
