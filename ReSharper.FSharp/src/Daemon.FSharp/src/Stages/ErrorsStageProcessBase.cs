using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Column = JetBrains.Util.dataStructures.TypedIntrinsics.Int32<JetBrains.DocumentModel.DocColumn>;
using Line = JetBrains.Util.dataStructures.TypedIntrinsics.Int32<JetBrains.DocumentModel.DocLine>;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
{
  public abstract class ErrorsStageProcessBase : FSharpDaemonStageProcessBase
  {
    [NotNull] private readonly FSharpErrorInfo[] myErrors;
    [NotNull] private readonly IDocument myDocument;

    /// https://github.com/fsharp/FSharp.Compiler.Service/blob/9.0.0/src/fsharp/CompileOps.fs#L246
    private const int ErrorNumberUndefined = 39;

    protected ErrorsStageProcessBase([NotNull] IDaemonProcess process, [NotNull] FSharpErrorInfo[] errors)
      : base(process)
    {
      myErrors = errors;
      myDocument = DaemonProcess.Document;
    }

    public override void Execute(Action<DaemonStageResult> committer)
    {
      var highlightings = new List<HighlightingInfo>(myErrors.Length);
      foreach (var error in myErrors)
      {
        var range = GetDocumentRange(myDocument, error);
        highlightings.Add(new HighlightingInfo(range, CreateHighlighting(error, range)));
        SeldomInterruptChecker.CheckForInterrupt();
      }
      committer(new DaemonStageResult(highlightings));
    }

    private static IHighlighting CreateHighlighting(FSharpErrorInfo error, DocumentRange range)
    {
      var message = error.Message;
      if (error.Severity.IsWarning) return new WarningHighlighting(message, range);
      if (error.ErrorNumber == ErrorNumberUndefined) return new UnresolvedHighlighting(message, range);
      return new ErrorHighlighting(message, range);
    }

    public static DocumentRange GetDocumentRange(IDocument document, FSharpErrorInfo error)
    {
      // Error in project options or environment
      if (error.StartLineAlternate == 0)
        return new DocumentRange(document, new TextRange(0, document.GetLineEndOffsetWithLineBreak(Line.O)));

      var startOffset = FSharpRangeUtil.GetDocumentOffset(document, (Line) (error.StartLineAlternate - 1),
        (Column) error.StartColumn);
      var endOffset = FSharpRangeUtil.GetDocumentOffset(document, (Line) (error.EndLineAlternate - 1),
        (Column) error.EndColumn);
      return new DocumentRange(document, new TextRange(startOffset, endOffset));
    }
  }
}