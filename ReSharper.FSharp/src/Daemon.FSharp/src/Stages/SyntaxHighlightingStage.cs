using System;
using System.Collections.Generic;
using JetBrains.Application;
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

    public class SyntaxHighlightStageProcess : FSharpDaemonStageProcessBase
    {
      private readonly IFSharpFile myFsFile;
      private readonly SeldomInterruptChecker myInterruptChecker;

      public SyntaxHighlightStageProcess(IFSharpFile fsFile, IDaemonProcess process) : base(process)
      {
        myFsFile = fsFile;
        myInterruptChecker = new SeldomInterruptChecker();
      }

      public override void Execute(Action<DaemonStageResult> committer)
      {
        var highlightings = new List<HighlightingInfo>();
        foreach (var token in myFsFile.Tokens())
        {
          if (token != null && token.GetTokenType() == FSharpTokenType.DEAD_CODE)
          {
            var range = token.GetNavigationRange();
            highlightings.Add(new HighlightingInfo(range, new DeadCodeHighlighting(range)));
          }
          myInterruptChecker.CheckForInterrupt();
        }
        committer(new DaemonStageResult(highlightings));
      }
    }
  }
}