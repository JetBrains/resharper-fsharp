using System;
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

      return RunFSharpAsync(Checker.ParseFileInProject(filePath, fileSource, projectOptions));
    }

    [CanBeNull]
    public static FSharpCheckFileResults CheckFSharpFile([NotNull] IFSharpFile fsFile,
      [CanBeNull] Action interruptChecker = null)
    {
      var sourceFile = fsFile.GetSourceFile();
      var projectOptions = sourceFile != null ? FSharpProjectOptionsProvider.GetProjectOptions(sourceFile) : null;
      if (fsFile.ParseResults == null || projectOptions == null) return null;

      var checkAsync = Checker.CheckFileInProject(fsFile.ParseResults, sourceFile.GetLocation().FullPath, 0,
        sourceFile.Document.GetText(), projectOptions, null, null);
      return (RunFSharpAsync(checkAsync, interruptChecker) as FSharpCheckFileAnswer.Succeeded)?.Item;
    }

    [CanBeNull]
    public static TResult RunFSharpAsync<TResult>(FSharpAsync<TResult> async,
      [CanBeNull] Action interruptChecker = null)
    {
      const int interruptCheckTimeout = 30;
      interruptChecker = interruptChecker ?? (() => InterruptableActivityCookie.CheckAndThrow());
      var cancellationTokenSource = new CancellationTokenSource();
      var cancellationToken = cancellationTokenSource.Token;
      var cancellationTokenOption = new FSharpOption<CancellationToken>(cancellationToken);
      var mres = new ManualResetEventSlim();

      var task = FSharpAsync.StartAsTask(AsyncUtil.RunAsyncAndSetEvent(async, mres), null, cancellationTokenOption);
      while (!task.IsCompleted)
      {
        var finished = mres.Wait(interruptCheckTimeout, cancellationToken);
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