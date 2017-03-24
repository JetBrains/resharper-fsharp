using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
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
        if (token == null)
          continue;

        var symbol = symbolUse.Symbol;
        var tokenType = token.GetTokenType();
        if ((tokenType == FSharpTokenType.GREATER || tokenType == FSharpTokenType.GREATER_RBRACK)
            && !IsOpGreaterThan(symbol))
          continue; // found usage of generic symbol with specified type parameter

        if (symbolUse.IsFromDefinition)
        {
          var declaration = FindDeclaration(symbol, token);
          if (declaration != null) declaration.Symbol = symbol;
          continue;
        }
        token.FSharpSymbol = symbol;
        SeldomInterruptChecker.CheckForInterrupt();
      }
      myFsFile.ReferencesResolved = true;
    }

    private static bool IsOpGreaterThan(FSharpSymbol symbol)
    {
      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      return mfv?.CompiledName == "op_GreaterThan";
    }

    [CanBeNull]
    private static IFSharpDeclaration FindDeclaration(FSharpSymbol symbol, ITreeNode token)
    {
      // todo: add other symbols (e.g let bindings, local values, type members), be careful with implicit constructors
      if (symbol is FSharpEntity)
      {
        return token.GetContainingNode<IFSharpDeclaration>();
      }
      if (symbol is FSharpField)
      {
        return token.GetContainingNode<IFSharpFieldDeclaration>();
      }
      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv == null)
        return null;

      if (mfv.IsMember || mfv.IsModuleValueOrMember)
        return (IFSharpDeclaration) token.GetContainingNode<ITypeMemberDeclaration>();

      return token.GetContainingNode<ILocalDeclaration>();
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