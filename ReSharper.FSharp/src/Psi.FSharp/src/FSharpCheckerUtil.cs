using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Progress;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Psi.FSharp
{
  public class FSharpCheckerUtil
  {
    public static readonly FSharpChecker Checker =
      FSharpChecker.Create(null, FSharpOption<bool>.Some(true), null, null);

    [CanBeNull]
    public static FSharpParseFileResults ParseFSharpFile([NotNull] IPsiSourceFile file)
    {
      var filePath = file.GetLocation().FullPath;
      var fileSource = file.Document.GetText();
      var projectOptions = FSharpProjectOptionsProvider.GetProjectOptions(file);

      return WaitForFSharpAsync(Checker.ParseFileInProject(filePath, fileSource, projectOptions));
    }

    public static TAsync WaitForFSharpAsync<TAsync>(FSharpAsync<TAsync> async, int interruptCheckTimeout = 30)
    {
      var interruptChecker = new SeldomInterruptCheckerWithCheckTime(interruptCheckTimeout);
      var cancellationTokenSource = new CancellationTokenSource();
      var cancellationToken = cancellationTokenSource.Token;
      var mres = new ManualResetEventSlim();

      InterruptableActivityCookie.CheckAndThrow();
      var checkTask = FSharpAsync.StartAsTask(async, null, new FSharpOption<CancellationToken>(cancellationToken));
      checkTask.ContinueWith(completed => { mres.Set(); }, TaskContinuationOptions.ExecuteSynchronously);

      while (!checkTask.IsCompleted)
      {
        var finished = mres.Wait(interruptCheckTimeout, cancellationToken);
        if (finished) break;
        try
        {
          interruptChecker.CheckForInterrupt();
        }
        catch (ProcessCancelledException)
        {
          cancellationTokenSource.Cancel();
          throw;
        }
      }
      return checkTask.Result;
    }
  }
}