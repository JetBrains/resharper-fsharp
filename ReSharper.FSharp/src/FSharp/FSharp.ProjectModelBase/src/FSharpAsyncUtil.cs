using System;
using System.Diagnostics;
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

    private static readonly FSharpReadLockRequestsQueue ReadRequests = new();

    [Conditional("JET_MODE_ASSERT")]
    public static void CheckAndThrow()
    {
      // If the FCS cancellation token is set, then this is FCS background analysis thread.
      // Otherwise, it's a R# background thread doing something under read lock.
      var isFcsThread = Cancellable.HasCancellationToken;
      Assertion.Assert(isFcsThread || Shell.Instance.Locks.IsReadAccessAllowed());
      if (isFcsThread)
      {
        try
        {
          Cancellable.CheckAndThrow();
        }
        catch (Exception e) when (e.IsOperationCanceled())
        {
          var logger = Logger.GetLogger(typeof(FSharpAsyncUtil));
          var fcsTokenCancelled = Cancellable.Token.IsCancellationRequested;
          logger.Trace($"Cancelled via Cancellable.CheckAndThrow, fcsToken.IsCancelled: {fcsTokenCancelled}");
          throw;
        }
      }
      else
      {
        {
          try
          {
            Interruption.Current.CheckAndThrow();
          }
          catch (Exception e) when (e.IsOperationCanceled())
          {
            var logger = Logger.GetLogger(typeof(FSharpAsyncUtil));
            logger.Trace("Cancelled via Interruption.Current.CheckAndThrow()");
            throw;
          }
        }

      }
    }

    /// <summary>
    /// Try to execute <paramref name="action"/> using read lock on the current thread.
    /// If not possible, queue a request to a thread calling FCS, possibly after a change on the main thread.
    /// When processing the request, the relevant psi module or declared element may already be removed and invalid.
    /// </summary>
    public static void UsingReadLockInsideFcs(IShellLocks locks, Action action)
    {
      var logger = Logger.GetLogger(typeof(FSharpAsyncUtil));

      // Capture FCS cancellation token so we can propagate cancellation in cases like F#->C#->F#,
      // where the last FCS request is initiated from inside reading the C# metadata,
      // and the initial request may get cancelled
      var fcsToken = Cancellable.HasCancellationToken ? Cancellable.Token : CancellationToken.None;

      // Try to acquire read lock on the current thread.
      // It should be possible, unless there's a write lock request that prevents it.
      try
      {
        // FCS token is the source for the cancellation
        using var _ = Interruption.Current.Add(new LifetimeInterruptionSource(fcsToken));
        if (locks.TryExecuteWithReadLock(action))
        {
          logger.Trace("UsingReadLockInsideFcs: executed on the initial thread, returning");
          return;
        }
      }
      catch (Exception e) when (e.IsOperationCanceled())
      {
        logger.Trace("UsingReadLockInsideFcs: exception: the operation cancelled on the initial thread");

        // todo: what if this happens in F#->C#->F#?
        if (locks.IsReadAccessAllowed())
        {
          // The FCS request has originated from a R# thread and was cancelled. We don't want to requeue this request.
          // If the request is coming from FCS, it's cancelled below in the loop.
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
      // To ensure FCS metadata consistency, we retry cancelled requests in this loop.
      // This may happen when R# read lock is cancelled, but the corresponding FCS request has not seen it yet.
      // We check the FCS request cancellation separately via its cancellation token.
      logger.Trace("UsingReadLockInsideFcs: before the loop");
      while (true)
      {
        CheckAndThrow();

        logger.Trace("UsingReadLockInsideFcs: enqueueing a new request");
        var tcs = new TaskCompletionSource<Unit>();
        ReadRequests.Enqueue(() =>
        {
          try
          {
            logger.Trace("UsingReadLockInsideFcs: before action");
            action();
            logger.Trace("UsingReadLockInsideFcs: after action");

            tcs.SetResult(Unit.Instance);
          }
          catch (Exception e) when (e.IsOperationCanceled())
          {
            tcs.SetCanceled();
            logger.Trace("UsingReadLockInsideFcs: exception: cancelled");
          }
          catch (Exception e)
          {
            tcs.SetException(e);
            logger.Trace("UsingReadLockInsideFcs: exception");
            Logger.LogException(e);
          }
        });

        try
        {
          logger.Trace("UsingReadLockInsideFcs: before tcs.Task.Wait");
          tcs.Task.Wait();
          logger.Trace("UsingReadLockInsideFcs: after tcs.Task.Wait, breaking");
          break;
        }
        catch (Exception e) when (e.IsOperationCanceled())
        {
          logger.Trace("UsingReadLockInsideFcs: exception: cancelled, checking FCS token");
          CheckAndThrow();
        }
        catch (Exception)
        {
          logger.Trace("UsingReadLockInsideFcs: exception");
          throw;
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
    public static TResult RunAsTask<TResult>([NotNull] this FSharpAsync<TResult> async)
    {
      var logger = Logger.GetLogger(typeof(FSharpAsyncUtil));

      using var cancellationSource = new FcsCancellationTokenSource();

      logger.Trace("RunAsTask: before StartAsTask");
      var task = FSharpAsync.StartAsTask(async, null, cancellationSource.Token);
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
        CheckAndThrow();

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

    /// Provides the token, depending on the request thread.
    /// When the request is coming from an FCS thread, then its cancellation token is propagated to the new request.
    /// Otherwise, and new lifetime definition (with a new token) is created. 
    private class FcsCancellationTokenSource : IDisposable
    {
      [CanBeNull] private readonly LifetimeDefinition myLifetimeDefinition;
      public CancellationToken Token { get; }

      public FcsCancellationTokenSource()
      {
        if (Cancellable.HasCancellationToken)
        {
          Token = Cancellable.Token;
          return;
        }

        myLifetimeDefinition = new LifetimeDefinition();
        Token = myLifetimeDefinition.Lifetime.ToCancellationToken();
      }

      public void Dispose() =>
        myLifetimeDefinition?.Dispose();
    }
  }
}
