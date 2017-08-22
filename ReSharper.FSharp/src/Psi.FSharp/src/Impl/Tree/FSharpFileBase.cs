using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class FSharpFileBase : FileElementBase, IFSharpFileCheckInfoOwner
  {
    private readonly object myCheckLock = new object();
    private readonly object myGetSymbolsLock = new object();
    private Dictionary<int, FSharpSymbol> myDeclarationSymbols;
    public FSharpCheckerService CheckerService { get; set; }
    public FSharpProjectOptions ProjectOptions { get; set; }
    public TokenBuffer ActualTokenBuffer { get; set; }
    public FSharpOption<FSharpParseFileResults> ParseResults { get; set; }
    public override PsiLanguageType Language => FSharpLanguage.Instance;
    public bool ReferencesResolved { get; set; }

    public FSharpOption<FSharpParseAndCheckResults> GetParseAndCheckResults([CanBeNull] Action interruptChecker = null,
      bool allowStaleResults = false)
    {
      lock (myCheckLock)
        return CheckerService.ParseAndCheckFile(GetSourceFile(), allowStaleResults);
    }

    public FSharpSymbol GetSymbolDeclaration(int offset)
    {
      lock (myGetSymbolsLock)
        if (myDeclarationSymbols == null)
        {
          var checkResults = GetParseAndCheckResults();
          var document = GetSourceFile()?.Document;
          var declaredSymbolUses = checkResults?.Value.CheckResults.GetAllUsesOfAllSymbolsInFile().RunAsTask();
          if (declaredSymbolUses == null || document == null)
            return null;

          myDeclarationSymbols = new Dictionary<int, FSharpSymbol>(declaredSymbolUses.Length);
          foreach (var symbolUse in declaredSymbolUses)
            if (symbolUse.IsFromDefinition)
            {
              // todo: check multiple symbols by same offset (e.g. entity & implicit ctor)
              var useOffset = document.GetOffset(symbolUse.RangeAlternate.Start);
              myDeclarationSymbols[useOffset] = symbolUse.Symbol;
            }
        }
      return myDeclarationSymbols?.TryGetValue(offset);
    }

    public virtual void Accept(TreeNodeVisitor visitor)
    {
      visitor.VisitNode(this);
    }

    public virtual void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context)
    {
      visitor.VisitNode(this, context);
    }

    public virtual TReturn Accept<TContext, TReturn>(TreeNodeVisitor<TContext, TReturn> visitor, TContext context)
    {
      return visitor.VisitNode(this, context);
    }
  }
}