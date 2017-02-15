using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
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

    public class IdentifiersHighlightStageProcess : FSharpHighlightStageProcessBase
    {
      public IdentifiersHighlightStageProcess([NotNull] IFSharpFile fsFile, [NotNull] IDaemonProcess process)
        : base(fsFile, process)
      {
      }

      public override void Execute(Action<DaemonStageResult> committer)
      {
        var highlightings = new List<HighlightingInfo>(FsFile.TokenBuffer.Buffer.Length);
        foreach (var token in FsFile.Tokens())
        {
          var symbol = (token as FSharpIdentifierToken)?.FSharpSymbol;
          if (symbol != null)
            highlightings.Add(CreateHighlighting(token, FSharpSymbolUtil.GetHighlightingAttributeId(symbol)));

          if (highlightings.Count % 100 == 0 && DaemonProcess.InterruptFlag) throw new ProcessCancelledException();
        }
        committer(new DaemonStageResult(highlightings));
      }
    }
  }
}