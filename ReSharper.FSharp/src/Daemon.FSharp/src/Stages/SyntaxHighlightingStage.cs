using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
{
  [DaemonStage(StagesBefore = new[] {typeof(GlobalFileStructureCollectorStage)},
    StagesAfter = new[] {typeof(SyntaxErrorsStage)})]
  public class SyntaxHighlightingStage : FSharpDaemonStageBase
  {
    protected override IDaemonStageProcess CreateProcess(IFSharpFile psiFile, IDaemonProcess process)
    {
      return new SyntaxHighlightStageProcess(psiFile, process);
    }

    public class SyntaxHighlightStageProcess : FSharpDaemonStageProcessBase
    {
      private readonly IFSharpFile myFsFile;

      public SyntaxHighlightStageProcess(IFSharpFile fsFile, IDaemonProcess process) : base(process)
      {
        myFsFile = fsFile;
      }

      public override void Execute(Action<DaemonStageResult> committer)
      {
        var highlightings = new List<HighlightingInfo>();
        foreach (var token in myFsFile.Tokens().OfType<FSharpDeadCodeToken>())
        {
          var range = token.GetNavigationRange();
          highlightings.Add(new HighlightingInfo(range, new DeadCodeHighlighting(range)));
          SeldomInterruptChecker.CheckForInterrupt();
        }
        committer(new DaemonStageResult(highlightings));
      }
    }
  }
}