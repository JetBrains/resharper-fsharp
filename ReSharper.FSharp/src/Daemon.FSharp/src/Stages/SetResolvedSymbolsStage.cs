using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
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
      var checkResults = myFsFile.GetParseAndCheckResults(interruptChecker)?.Value.CheckResults;
      var symbolUses = checkResults?.GetAllUsesOfAllSymbolsInFile()?.RunAsTask(interruptChecker);
      if (symbolUses == null)
        return;

      if (myFsFile.ReferencesResolved)
      {
        myFsFile.ReferencesResolved = false;
        foreach (var token in myFsFile.Tokens().OfType<FSharpIdentifierToken>())
          token.FSharpSymbol = null;
      }

      var highlightings = new List<HighlightingInfo>(symbolUses.Length);

      foreach (var symbolUse in symbolUses)
      {
        // range includes qualifiers, we're looking for the last identifier
        var useRangeEnd = myDocument.GetTreeEndOffset(symbolUse.RangeAlternate) - 1;
        var token = myFsFile.FindTokenAt(useRangeEnd) as FSharpIdentifierToken;
        if (token == null)
          continue;

        var shouldHighlight = token.FSharpSymbol == null;
        var symbol = symbolUse.Symbol;
        var highlightingId = symbol.GetHighlightingAttributeId();

        if (token.GetTokenType() == FSharpTokenType.GREATER && !symbol.IsOpGreaterThan())
          continue; // found usage of generic type with specified type parameter

        if (symbolUse.IsFromDefinition)
        {
          var declaration = FindDeclaration(symbol, token);
          if (declaration != null)
          {
            shouldHighlight |= declaration.Symbol == null;
            if (declaration.Symbol == null || ShouldReplaceSymbol(declaration.Symbol, symbol))
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

        if (token.FSharpSymbol == null || ShouldReplaceSymbol(token.FSharpSymbol, symbol))
          token.FSharpSymbol = symbol;
        if (shouldHighlight)
          highlightings.Add(CreateHighlighting(token, highlightingId));

        SeldomInterruptChecker.CheckForInterrupt();
      }
      myFsFile.ReferencesResolved = true;
      committer(new DaemonStageResult(highlightings));
    }

    private static bool ShouldReplaceSymbol(FSharpSymbol setSymbol, FSharpSymbol newSymbol)
    {
      var setEntity = setSymbol as FSharpEntity;
      var newMfv = newSymbol as FSharpMemberOrFunctionOrValue;
      if (setEntity != null && newMfv != null && (newMfv.IsConstructor || newMfv.IsImplicitConstructor))
        return false;

      return true;
    }

    [CanBeNull]
    private static IFSharpDeclaration FindDeclaration(FSharpSymbol symbol, ITreeNode token)
    {
      // todo: add other symbols (e.g let bindings, local values, type members), be careful with implicit constructors
      if (symbol is FSharpEntity)
        return token.GetContainingNode<IFSharpDeclaration>();

      if (symbol is FSharpField)
        return token.GetContainingNode<IFSharpDeclaration>();

      if (symbol is FSharpUnionCase)
        return token.GetContainingNode<IFSharpDeclaration>();

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
  }
}