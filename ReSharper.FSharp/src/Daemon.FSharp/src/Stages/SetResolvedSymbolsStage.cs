using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Daemon.FSharp.Stages
{
  [DaemonStage(StagesBefore = new[] {typeof(SyntaxErrorsStage)}, StagesAfter = new[] {typeof(TypeCheckErrorsStage)})]
  public class SetResolvedSymbolsStage : FSharpDaemonStageBase
  {
    protected override IDaemonStageProcess CreateProcess(IFSharpFile psiFile, IDaemonProcess process)
    {
      return new SetResolvedSymbolsStageProcess(psiFile, process);
    }
  }

  public class SetResolvedSymbolsStageProcess : FSharpDaemonStageProcessBase
  {
    private readonly IFSharpFile myFsFile;
    private readonly IDocument myDocument;

    public SetResolvedSymbolsStageProcess([NotNull] IFSharpFile fsFile, [NotNull] IDaemonProcess process)
      : base(process)
    {
      myFsFile = fsFile;
      myDocument = process.Document;
    }

    public override void Execute(Action<DaemonStageResult> committer)
    {
      var interruptChecker = DaemonProcess.CreateInterruptChecker();
      var symbolUses = myFsFile.GetCheckResults(interruptChecker)
        ?.GetAllUsesOfAllSymbolsInFile()
        ?.RunAsTask(interruptChecker);
      if (symbolUses == null) return;

      foreach (var symbolUse in symbolUses)
      {
        var token = FindUsageToken(symbolUse);
        if (token == null) continue;

        if (symbolUse.IsFromDefinition)
        {
          // todo: add other symbols (e.g let bindings, local values, type members), be careful with implicit constructors
          if (symbolUse.Symbol is FSharpEntity)
          {
            var declaration = token.GetContainingNode<IFSharpDeclaration>();
            if (declaration != null) declaration.Symbol = symbolUse.Symbol;
          }
          continue;
        }
        token.FSharpSymbol = symbolUse.Symbol;
        SeldomInterruptChecker.CheckForInterrupt();
      }
      myFsFile.ReferencesResolved = true;
    }

    [CanBeNull]
    private FSharpIdentifierToken FindUsageToken(FSharpSymbolUse symbolUse)
    {
      var name = FSharpNamesUtil.GetDisplayName(symbolUse.Symbol);
      if (name == null) return null;

      // range includes qualifiers, we're looking for the last identifier
      var endOffset = FSharpRangeUtil.GetEndOffset(myDocument, symbolUse.RangeAlternate) - 1;
      return myFsFile.FindTokenAt(endOffset) as FSharpIdentifierToken;
    }
  }
}