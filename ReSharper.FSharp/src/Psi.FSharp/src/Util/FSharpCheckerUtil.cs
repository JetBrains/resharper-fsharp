using System;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Progress;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  public class FSharpCheckerUtil
  {
    public static readonly FSharpChecker Checker =
      FSharpChecker.Create(projectCacheSize: null, keepAssemblyContents: FSharpOption<bool>.Some(true),
        keepAllBackgroundResolutions: null, msbuildEnabled: new FSharpOption<bool>(false));

    [CanBeNull]
    public static FSharpParseFileResults ParseFSharpFile([NotNull] IPsiSourceFile file)
    {
      var filePath = file.GetLocation().FullPath;
      var fileSource = file.Document.GetText();
      var projectOptions = FSharpProjectOptionsProvider.GetProjectOptions(file);

      return RunFSharpAsync(Checker.ParseFileInProject(filePath, fileSource, projectOptions));
    }

    [CanBeNull]
    public static FSharpCheckFileResults CheckFSharpFile([NotNull] IPsiSourceFile sourceFile,
      [NotNull] FSharpParseFileResults parseResults, [CanBeNull] Action interruptChecker = null)
    {
      var projectOptions = FSharpProjectOptionsProvider.GetProjectOptions(sourceFile);
      var checkAsync = Checker.CheckFileInProject(parseResults, sourceFile.GetLocation().FullPath, 0,
        sourceFile.Document.GetText(), projectOptions, isResultObsolete: null, textSnapshotInfo: null);
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