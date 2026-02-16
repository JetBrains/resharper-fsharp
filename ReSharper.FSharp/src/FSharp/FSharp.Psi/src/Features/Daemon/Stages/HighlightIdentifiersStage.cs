using System;
using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
{
  [DaemonStage(Instantiation.DemandAnyThreadSafe, StagesAfter = [typeof(CollectUsagesStage)])]
  public class HighlightIdentifiersStage() : FSharpDaemonStageBase(visibleDocumentsOnly: true)
  {
    protected override IDaemonStageProcess CreateStageProcess(IFSharpFile psiFile, IContextBoundSettingsStore settings,
      IDaemonProcess process, DaemonProcessKind _) =>
      new HighlightIdentifiersStageProcess(psiFile, process);
  }

  public class HighlightIdentifiersStageProcess([NotNull] IFSharpFile fsFile, [NotNull] IDaemonProcess process)
    : FSharpDaemonStageProcessBase(fsFile, process)
  {
    private readonly IDocument myDocument = process.Document;

    private void AddHighlightings(IEnumerable<FcsResolvedSymbolUse> symbolsUses,
      ICollection<HighlightingInfo> highlightings)
    {
      foreach (var resolvedSymbolUse in symbolsUses)
      {
        var symbolUse = resolvedSymbolUse.SymbolUse;
        var symbol = symbolUse.Symbol;

        var highlightingId =
          symbolUse.IsFromComputationExpression
            ? FSharpHighlightingAttributeIdsModule.ComputationExpression
            : symbol.GetHighlightingAttributeId();

        var documentRange = new DocumentRange(myDocument, resolvedSymbolUse.Range);

        if (symbol is FSharpMemberOrFunctionOrValue mfv)
        {
          if (symbolUse.IsFromDefinition)
          {
            if (mfv.LogicalName == StandardMemberNames.Constructor &&
                myDocument.Buffer.GetText(resolvedSymbolUse.Range) == "new")
              continue;

            if (mfv.IsActivePattern && !FSharpFile.IsFSharpSigFile())
              continue;
          }

          if (documentRange.Length == 3 && mfv.LogicalName == "op_Multiply" && 
              myDocument.Buffer.GetText(resolvedSymbolUse.Range) == "(*)")
            documentRange = documentRange.TrimLeft(1).TrimRight(1);
        }

        var highlighting = new FSharpIdentifierHighlighting(highlightingId, documentRange);
        highlightings.Add(new HighlightingInfo(documentRange, highlighting));

        Interruption.Current.CheckAndThrow();
      }
    }

    public override void Execute(Action<DaemonStageResult> committer)
    {
      var declarations = FSharpFile.GetAllDeclaredSymbols();
      Interruption.Current.CheckAndThrow();

      var usages = FSharpFile.GetAllResolvedSymbols();
      Interruption.Current.CheckAndThrow();

      var highlightings = new List<HighlightingInfo>(declarations.Count + usages.Count);
      AddHighlightings(declarations, highlightings);
      AddHighlightings(usages, highlightings);
      committer(new DaemonStageResult(highlightings.AsIReadOnlyList()));
    }
  }
}
