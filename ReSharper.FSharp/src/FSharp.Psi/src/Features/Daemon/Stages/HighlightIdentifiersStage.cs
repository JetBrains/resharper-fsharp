using System;
using System.Collections.Generic;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
{
  [DaemonStage(StagesAfter = new[] {typeof(CollectUsagesStage)})]
  public class HighlightIdentifiersStage : FSharpDaemonStageBase
  {
    protected override bool IsSupported(IPsiSourceFile sourceFile, DaemonProcessKind processKind) =>
      processKind == DaemonProcessKind.VISIBLE_DOCUMENT && base.IsSupported(sourceFile, processKind);

    protected override IDaemonStageProcess CreateStageProcess(IFSharpFile psiFile, IContextBoundSettingsStore settings,
      IDaemonProcess process) =>
      new HighlightIdentifiersStageProcess(psiFile, process);
  }

  public class HighlightIdentifiersStageProcess : FSharpDaemonStageProcessBase
  {
    private readonly IDocument myDocument;

    public HighlightIdentifiersStageProcess([NotNull] IFSharpFile fsFile, [NotNull] IDaemonProcess process)
      : base(fsFile, process) => myDocument = process.Document;

    private void AddHighlightings(IEnumerable<FSharpResolvedSymbolUse> symbolsUses,
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
    }

    public override void Execute(Action<DaemonStageResult> committer)
    {
      var declarations = FSharpFile.GetAllDeclaredSymbols();
      InterruptableActivityCookie.CheckAndThrow();

      var usages = FSharpFile.GetAllResolvedSymbols();
      InterruptableActivityCookie.CheckAndThrow();

      var highlightings = new List<HighlightingInfo>(declarations.Count + usages.Count);
      AddHighlightings(declarations, highlightings);
      AddHighlightings(usages, highlightings);
      committer(new DaemonStageResult(highlightings.AsIReadOnlyList()));
    }
  }
}
