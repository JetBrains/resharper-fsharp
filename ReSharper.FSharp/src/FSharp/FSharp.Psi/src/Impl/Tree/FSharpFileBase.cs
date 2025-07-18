﻿using System;
using System.Collections.Generic;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using FSharp.Compiler.Syntax;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class FSharpFileBase : FileElementBase, IFSharpFileCheckInfoOwner
  {
    // ReSharper disable once NotNullMemberIsNotInitialized
    public FcsCheckerService FcsCheckerService { get; set; }
    public FcsCheckerService CheckerService => FcsCheckerService;

    public IFcsFileCapturedInfo FcsCapturedInfo =>
      FcsCapturedInfoCache.GetOrCreateFileCapturedInfo(SourceFile);

    // ReSharper disable once NotNullMemberIsNotInitialized
    public IFcsCapturedInfoCache FcsCapturedInfoCache { get; set; }

    private readonly CachedPsiValue<FSharpOption<FSharpParseFileResults>> myParseResults =
      new FileCachedPsiValue<FSharpOption<FSharpParseFileResults>>();

    public FSharpOption<FSharpParseFileResults> ParseResults
    {
      get => myParseResults.GetValue(this, static fsFile => fsFile.FcsCheckerService.ParseFile(fsFile.SourceFile));
      set => myParseResults.SetValue(this, value);
    }

    public FSharpOption<ParsedInput> ParseTree =>
      ParseResults?.Value.ParseTree is { } parseTree
        ? FSharpOption<ParsedInput>.Some(parseTree)
        : FSharpOption<ParsedInput>.None;

    PsiLanguageType IFSharpFileCheckInfoOwner.LanguageType { get; set; }

    public override PsiLanguageType Language => ((IFSharpFileCheckInfoOwner) this).LanguageType;

    public FSharpOption<FSharpParseAndCheckResults> GetParseAndCheckResults(bool allowStaleResults, string opName) =>
      FcsCheckerService.ParseAndCheckFile(SourceFile, opName, allowStaleResults);

    public IDisposable PinTypeCheckResults(bool prohibitTypeCheck, string opName) =>
      CheckerService.PinCheckResults(SourceFile, prohibitTypeCheck, opName);

    public IDisposable PinTypeCheckResults(FSharpOption<FSharpParseAndCheckResults> results) =>
      CheckerService.PinCheckResults(results, SourceFile, true);

    public FSharpSymbol GetSymbol(int offset) =>
      FcsCapturedInfo.GetSymbol(offset);

    public IReadOnlyList<FcsResolvedSymbolUse> GetAllResolvedSymbols(FSharpCheckFileResults checkResults = null) =>
      FcsCapturedInfo.GetAllResolvedSymbols();

    public IReadOnlyList<FcsResolvedSymbolUse> GetAllDeclaredSymbols(FSharpCheckFileResults checkResults = null) =>
      FcsCapturedInfo.GetAllDeclaredSymbols();

    public FSharpSymbolUse GetSymbolUse(int offset) =>
      FcsCapturedInfo.GetSymbolUse(offset);

    public FSharpSymbolUse GetSymbolDeclaration(int offset) =>
      FcsCapturedInfo.GetSymbolDeclaration(offset);

    public IDocument StandaloneDocument { get; set; }

    public virtual void Accept(TreeNodeVisitor visitor) => visitor.VisitNode(this);

    public virtual void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context) =>
      visitor.VisitNode(this, context);

    public virtual TReturn Accept<TContext, TReturn>(TreeNodeVisitor<TContext, TReturn> visitor, TContext context) =>
      visitor.VisitNode(this, context);

    [NotNull] public IFSharpFile FSharpFile => (IFSharpFile) this;
    [NotNull] public IPsiSourceFile SourceFile => GetSourceFile().NotNull();
  }
}
