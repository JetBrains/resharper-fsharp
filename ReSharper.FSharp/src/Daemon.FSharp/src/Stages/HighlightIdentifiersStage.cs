using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
{
  [DaemonStage(StagesAfter = new[] {typeof(CollectUsagesStage)})]
  public class HighlightIdentifiersStage : FSharpDaemonStageBase
  {
    protected override IDaemonStageProcess CreateProcess(IFSharpFile psiFile, IDaemonProcess process) =>
      new HighlightIdentifiersStageProcess(psiFile, process);
  }

  public class HighlightIdentifiersStageProcess : FSharpDaemonStageProcessBase
  {
    private readonly IFSharpFile myFsFile;
    private readonly IDocument myDocument;

    public HighlightIdentifiersStageProcess([NotNull] IFSharpFile fsFile, [NotNull] IDaemonProcess process)
      : base(process)
    {
      myFsFile = fsFile;
      myDocument = process.Document;
    }

    private void HighlightUses(Action<DaemonStageResult> committer, IEnumerable<FSharpResolvedSymbolUse> symbols, int allSymbolsCount)
    {
      var highlightings = new List<HighlightingInfo>(allSymbolsCount);
      foreach (var resolvedSymbolUse in symbols)
      {
        var symbolUse = resolvedSymbolUse.SymbolUse;
        var symbol = symbolUse.Symbol;

        var highlightingId =
          symbolUse.IsFromComputationExpression
            ? HighlightingAttributeIds.KEYWORD
            : symbol.GetHighlightingAttributeId();

        if (symbolUse.IsFromDefinition && symbol is FSharpMemberOrFunctionOrValue mfv)
        {
          if (myDocument.Buffer.GetText(resolvedSymbolUse.Range) == "new" &&
              mfv.LogicalName == StandardMemberNames.Constructor)
            continue;
        }

        var documentRange = new DocumentRange(myDocument, resolvedSymbolUse.Range);
        var highlighting = new FSharpIdentifierHighlighting(highlightingId, documentRange);
        highlightings.Add(new HighlightingInfo(documentRange, highlighting));

        SeldomInterruptChecker.CheckForInterrupt();
      }
      committer(new DaemonStageResult(highlightings));
    }

    public override void Execute(Action<DaemonStageResult> committer)
    {
      // todo: add more cases to GetSemanticClassification (e.g. methods, values, namespaces) and use it instead?
      var checkResults = DaemonProcess.CustomData.GetData(FSharpDaemonStageBase.TypeCheckResults);
      var declarations = myFsFile.GetAllDeclaredSymbols(checkResults?.Value);
      SeldomInterruptChecker.CheckForInterrupt();

      var usages = myFsFile.GetAllResolvedSymbols();
      SeldomInterruptChecker.CheckForInterrupt();

      HighlightUses(committer,  declarations.Concat(usages), declarations.Length + usages.Length);
    }
  }
}