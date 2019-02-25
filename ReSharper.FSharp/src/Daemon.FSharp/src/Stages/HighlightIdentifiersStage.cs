using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.Util.DataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
{
  [DaemonStage(StagesAfter = new[] {typeof(CollectUsagesStage)})]
  public class HighlightIdentifiersStage : FSharpDaemonStageBase
  {
    protected override IDaemonStageProcess CreateStageProcess(IFSharpFile psiFile, IContextBoundSettingsStore settings,
      IDaemonProcess process) =>
      new HighlightIdentifiersStageProcess(psiFile, process);
  }

  public class HighlightIdentifiersStageProcess : FSharpDaemonStageProcessBase
  {
    private readonly IDocument myDocument;

    public HighlightIdentifiersStageProcess([NotNull] IFSharpFile fsFile, [NotNull] IDaemonProcess process)
      : base(fsFile, process) => myDocument = process.Document;

    private void HighlightUses(Action<DaemonStageResult> committer,
      IEnumerable<IReadOnlyList<FSharpResolvedSymbolUse>> allSymbols, int allSymbolsCount)
    {
      var highlightings = new ChunkList<HighlightingInfo>(allSymbolsCount);
      foreach (var symbols in allSymbols)
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
          if (mfv.LogicalName == StandardMemberNames.Constructor &&
              myDocument.Buffer.GetText(resolvedSymbolUse.Range) == "new")
            continue;

          if (mfv.IsActivePattern)
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
      var declarations = FSharpFile.GetAllDeclaredSymbols();
      InterruptableActivityCookie.CheckAndThrow();

      var usages = FSharpFile.GetAllResolvedSymbols();
      InterruptableActivityCookie.CheckAndThrow();

      HighlightUses(committer, new[] {declarations, usages}, declarations.Count + usages.Count);
    }
  }
}
