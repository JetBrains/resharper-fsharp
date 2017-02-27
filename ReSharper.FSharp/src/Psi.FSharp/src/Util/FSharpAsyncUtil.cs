using System;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Progress;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  public static class FSharpAsyncUtil
  {
    [CanBeNull]
    public static TResult RunAsTask<TResult>([NotNull] this FSharpAsync<TResult> async,
      [CanBeNull] Action interruptChecker = null)
    {
      const int interruptCheckTimeout = 30;
      interruptChecker = interruptChecker ?? (() => InterruptableActivityCookie.CheckAndThrow());
      var cancellationTokenSource = new CancellationTokenSource();
      var cancellationToken = cancellationTokenSource.Token;
      var cancellationTokenOption = new FSharpOption<CancellationToken>(cancellationToken);

      var task = FSharpAsync.StartAsTask(async, null, cancellationTokenOption);
      while (!task.IsCompleted)
      {
        var finished = task.Wait(interruptCheckTimeout, cancellationToken);
        if (finished) break;
        try
        {
          interruptChecker();
        }
        catch (ProcessCancelledException)
        {
          cancellationTokenSource.Cancel();
          throw;
        }
      }
      return task.Result;
    }
  }
}