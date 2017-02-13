using System;
using System.Collections.Generic;
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

    public class SyntaxHighlightStageProcess : IDaemonStageProcess
    {
      private readonly IFSharpFile myFsFile;

      public SyntaxHighlightStageProcess(IFSharpFile fsFile, IDaemonProcess process)
      {
        myFsFile = fsFile;
        DaemonProcess = process;
      }

      public void Execute(Action<DaemonStageResult> committer)
      {
        var highlightings = new List<HighlightingInfo>();
        foreach (var token in myFsFile.Tokens())
        {
          var tokenType = token.GetTokenType();
          if (tokenType == FSharpTokenType.DEAD_CODE)
            highlightings.Add(CreateTokenHighlighting(token, HighlightingAttributeIds.DEADCODE_ATTRIBUTE));
          else if (tokenType == FSharpTokenType.IDENTIFIER || tokenType == FSharpTokenType.OPERATOR)
          {
            var attrId = HighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE; // todo: get from resolved symbol
            highlightings.Add(CreateTokenHighlighting(token, attrId));
          }
        }
        committer(new DaemonStageResult(highlightings));
      }

      private static HighlightingInfo CreateTokenHighlighting(ITreeNode token, string highlightingAttributeId)
      {
        var range = token.GetNavigationRange();
        var highlighting = new FSharpIdentifierHighlighting(highlightingAttributeId, range);
        return new HighlightingInfo(range, highlighting);
      }

      public IDaemonProcess DaemonProcess { get; }
    }
  }
}