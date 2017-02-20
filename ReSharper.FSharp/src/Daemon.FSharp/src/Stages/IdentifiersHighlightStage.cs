using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.FSharp.Highlightings;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Daemon.FSharp.Stages
{
  [DaemonStage(StagesBefore = new[] {typeof(TypeCheckErrorsStage)}, StagesAfter = new[] {typeof(CollectUsagesStage)})]
  public class IdentifiersHighlightStage : FSharpDaemonStageBase
  {
    protected override IDaemonStageProcess CreateProcess(IFSharpFile fsFile, IDaemonProcess process)
    {
      return new IdentifiersHighlightStageProcess(fsFile, process);
    }

    public class IdentifiersHighlightStageProcess : FSharpDaemonStageProcessBase
    {
      private readonly IFSharpFile myFsFile;

      public IdentifiersHighlightStageProcess([NotNull] IFSharpFile fsFile, [NotNull] IDaemonProcess process)
        : base(process)
      {
        myFsFile = fsFile;
      }

      public override void Execute(Action<DaemonStageResult> committer)
      {
        var highlightings = new List<HighlightingInfo>(myFsFile.TokenBuffer.Buffer.Length);
        foreach (var token in myFsFile.Tokens())
        {
          var symbol = (token as FSharpIdentifierToken)?.FSharpSymbol;
          if (symbol != null) highlightings.Add(CreateHighlighting(token, symbol.GetHighlightingAttributeId()));
          SeldomInterruptChecker.CheckForInterrupt();
        }
        committer(new DaemonStageResult(highlightings));
      }

      protected static HighlightingInfo CreateHighlighting(ITreeNode token, string highlightingAttributeId)
      {
        var range = token.GetNavigationRange();
        var highlighting = new FSharpIdentifierHighlighting(highlightingAttributeId, range);
        return new HighlightingInfo(range, highlighting);
      }
    }
  }
}