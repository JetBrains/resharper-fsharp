using System;
using System.Collections.Generic;
using System.Linq;
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
      var symbolUses = myFsFile.GetCheckResults(true, interruptChecker)
        ?.GetAllUsesOfAllSymbolsInFile()
        ?.RunAsTask(interruptChecker);
      if (symbolUses == null)
        return;

      myFsFile.ReferencesResolved = false;
      foreach (var token in myFsFile.Tokens().OfType<FSharpIdentifierToken>())
        token.FSharpSymbol = null;

      var highlightings = new List<HighlightingInfo>(symbolUses.Length);

      foreach (var symbolUse in symbolUses)
      {
        var token = FindUsageToken(symbolUse);
        if (token == null)
          continue;

        var shouldHighlight = token.FSharpSymbol == null;
        var symbol = symbolUse.Symbol;
        var highlightingId = symbol.GetHighlightingAttributeId();
        var tokenType = token.GetTokenType();
        if ((tokenType == FSharpTokenType.GREATER || tokenType == FSharpTokenType.GREATER_RBRACK)
            && !FSharpSymbolsUtil.IsOpGreaterThan(symbol))
          continue; // found usage of generic symbol with specified type parameter

        if (symbolUse.IsFromDefinition)
        {
          var declaration = FindDeclaration(symbol, token);
          if (declaration != null)
          {
            declaration.Symbol = symbol;
            if (shouldHighlight)
              highlightings.Add(CreateHighlighting(token, highlightingId));
          }
          continue;
        }

        // usage of symbol may override declaration (e.g. interface member)
        // todo: better check?
        var parentDeclaration = token.GetContainingNode<ITypeMemberDeclaration>();
        if (parentDeclaration != null && parentDeclaration.GetNameRange() == token.GetTreeTextRange())
          continue;

        token.FSharpSymbol = symbol;
        if (shouldHighlight)
          highlightings.Add(CreateHighlighting(token, highlightingId));

        SeldomInterruptChecker.CheckForInterrupt();
      }
      myFsFile.ReferencesResolved = true;
      committer(new DaemonStageResult(highlightings));
    }

    [CanBeNull]
    private static IFSharpDeclaration FindDeclaration(FSharpSymbol symbol, ITreeNode token)
    {
      // todo: add other symbols (e.g let bindings, local values, type members), be careful with implicit constructors
      if (symbol is FSharpEntity)
        return token.GetContainingNode<IFSharpDeclaration>();

      if (symbol is FSharpField)
        return token.GetContainingNode<IFSharpFieldDeclaration>();

      if (symbol is FSharpUnionCase)
        return token.GetContainingNode<IFSharpUnionCaseDeclaration>();

      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null)
      {
        if (mfv.IsImplicitConstructor)
          return null;

        if (mfv.IsModuleValueOrMember)
        {
          var memberDeclaration = token.GetContainingNode<ITypeMemberDeclaration>();
          return (IFSharpDeclaration) (memberDeclaration is ITypeDeclaration ? null : memberDeclaration);
        }
      }

      return token.GetContainingNode<ILocalDeclaration>();
    }

    [CanBeNull]
    private FSharpIdentifierToken FindUsageToken(FSharpSymbolUse symbolUse)
    {
      var name = FSharpNamesUtil.GetDisplayName(symbolUse.Symbol);
      if (name == null) return null;

      // range includes qualifiers, we're looking for the last identifier
      var endOffset = myDocument.GetTreeEndOffset(symbolUse.RangeAlternate) - 1;
      return myFsFile.FindTokenAt(endOffset) as FSharpIdentifierToken;
    }
  }
}