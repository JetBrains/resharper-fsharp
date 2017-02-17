using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Daemon.FSharp.Highlightings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Daemon.FSharp.Stages
{
  [DaemonStage(StagesBefore = new[] {typeof(GlobalFileStructureCollectorStage)},
    StagesAfter = new[] {typeof(SyntaxErrorsStage)})]
  public class SyntaxHighlightingStage : FSharpDaemonStageBase
  {
    protected override IDaemonStageProcess CreateProcess(IFSharpFile psiFile, IDaemonProcess process)
    {
      return new SyntaxHighlightStageProcess(psiFile, process);
    }

    public class SyntaxHighlightStageProcess : FSharpHighlightStageProcessBase
    {
      public SyntaxHighlightStageProcess(IFSharpFile fsFile, IDaemonProcess process) : base(fsFile, process)
      {
      }

      public override void Execute(Action<DaemonStageResult> committer)
      {
        var highlightings = new List<HighlightingInfo>();
        using (var tokensEnumerator = FsFile.Tokens().GetEnumerator())
        {
          var token = tokensEnumerator.Current;
          for (var i = 0; tokensEnumerator.MoveNext(); i++)
          {
            if (token != null && token.GetTokenType() == FSharpTokenType.DEAD_CODE)
            {
              var range = token.GetNavigationRange();
              highlightings.Add(new HighlightingInfo(range, new DeadCodeHighlighting(range)));
            }
            token = tokensEnumerator.Current;
            if (i % 100 == 0 && DaemonProcess.InterruptFlag) throw new ProcessCancelledException();
          }
        }
        committer(new DaemonStageResult(highlightings));
      }
    }
  }
}