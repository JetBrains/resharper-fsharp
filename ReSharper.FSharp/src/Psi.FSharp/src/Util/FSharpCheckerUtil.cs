using System.Threading;
using FSharpUtil;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  public class FSharpCheckerUtil
  {
    public static readonly FSharpChecker Checker =
      FSharpChecker.Create(null, FSharpOption<bool>.Some(true), null, new FSharpOption<bool>(false));

    [CanBeNull]
    public static FSharpParseFileResults ParseFSharpFile([NotNull] IPsiSourceFile file)
    {
      var filePath = file.GetLocation().FullPath;
      var fileSource = file.Document.GetText();
      var projectOptions = FSharpProjectOptionsProvider.GetProjectOptions(file);

      return WaitForFSharpAsync(Checker.ParseFileInProject(filePath, fileSource, projectOptions));
    }

    [CanBeNull]
    public static FSharpCheckFileResults CheckFSharpFile([NotNull] IFSharpFile fsFile)
    {
      var sourceFile = fsFile.GetSourceFile();
      var projectOptions = sourceFile != null ? FSharpProjectOptionsProvider.GetProjectOptions(sourceFile) : null;
      if (fsFile.ParseResults == null || projectOptions == null) return null;

      var checkAsync = Checker.CheckFileInProject(fsFile.ParseResults, sourceFile.GetLocation().FullPath, 0,
        sourceFile.Document.GetText(), projectOptions, null);
      var checkResultsAnswer = WaitForFSharpAsync(checkAsync) as FSharpCheckFileAnswer.Succeeded;
      return checkResultsAnswer?.Item;
    }

    public static TAsync WaitForFSharpAsync<TAsync>(FSharpAsync<TAsync> async, int interruptCheckTimeout = 30)
    {
      var interruptChecker = InterruptableActivityCookie.GetCheck();
      var cancellationTokenSource = new CancellationTokenSource();
      var cancellationToken = cancellationTokenSource.Token;
      var mres = new ManualResetEventSlim();

      InterruptableActivityCookie.CheckAndThrow();
      var task = FSharpAsync.StartAsTask(AsyncUtil.RunAsyncAndSetEvent(async, mres), null,
        new FSharpOption<CancellationToken>(cancellationToken));
      while (!task.IsCompleted)
      {
        var finished = mres.Wait(interruptCheckTimeout, cancellationToken);
        if (finished) break;
        try
        {
          interruptChecker?.Invoke();
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