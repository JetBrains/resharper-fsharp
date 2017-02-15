using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.Util.Extension;
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
    private const string AttributeSuffix = "Attribute";

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
      var checkResult = myFsFile.CheckResults;
      if (checkResult == null) return;

      var symbolUses = FSharpCheckerUtil.WaitForFSharpAsync(checkResult.GetAllUsesOfAllSymbolsInFile());
      if (DaemonProcess.InterruptFlag) throw new ProcessCancelledException();

      for (var i = 0; i < symbolUses.Length; i++)
      {
        var symbolUse = symbolUses[i]; // todo: remove for declarations
        var token = FindUsageToken(symbolUse);
        if (token != null) token.FSharpSymbol = symbolUse.Symbol;

        if (i % 100 == 0 && DaemonProcess.InterruptFlag) throw new ProcessCancelledException();
      }
      myFsFile.ReferencesResolved = true;
    }

    [CanBeNull]
    private FSharpIdentifierToken FindUsageToken(FSharpSymbolUse symbolUse)
    {
      var name = FSharpSymbolUtil.GetDisplayName(symbolUse.Symbol);
      if (name == null) return null;

      // range includes qualifiers so we need the last identifier only
      var endOffset = FSharpRangeUtil.GetEndOffset(myDocument, symbolUse.RangeAlternate) - 1;
      var token = myFsFile.FindTokenAt(endOffset) as FSharpIdentifierToken;
      if (token == null) return null;

      if (name.Length == token.Length) return token;

      // "Some" or "SomeAttribute" in element attribute, "SomeAttribute" elsewhere
      var attrName = symbolUse.IsFromAttribute ? name.SubstringBeforeLast(AttributeSuffix) : null;
      if (attrName != null && attrName.Length == token.Length) return token;

      // e.g. name: "( |> )", token: "|>"
      if (FSharpSymbolUtil.IsEscapedName(name) && name.Length - 4 == token.Length) return token;

      // e.g. name: "foo bar", token: "``foo bar``"
      if (name.Length + 4 == token.Length && name == token.GetText().Substring(2, token.Length - 4)) return token;

      return null;
    }
  }
}