using System;
using System.Collections.Generic;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using FSharp.Compiler.Syntax;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IFSharpFileCheckInfoOwner : ICompositeElement
  {
    [CanBeNull]
    FSharpOption<FSharpParseAndCheckResults> GetParseAndCheckResults(bool allowStaleResults, string opName);

    IDisposable PinTypeCheckResults(bool prohibitTypeCheck, string opName);

    IDisposable PinTypeCheckResults(FSharpOption<FSharpParseAndCheckResults> results);

    [NotNull] FcsCheckerService FcsCheckerService { get; set; }

    [NotNull] IFcsFileCapturedInfo FcsCapturedInfo { get; }
    [NotNull] IFcsCapturedInfoCache FcsCapturedInfoCache { get; set; }

    [CanBeNull] FSharpOption<FSharpParseFileResults> ParseResults { get; set; }

    [CanBeNull] FSharpOption<ParsedInput> ParseTree { get; }

    [CanBeNull]
    FSharpSymbolUse GetSymbolUse(int offset);

    [CanBeNull]
    FSharpSymbolUse GetSymbolDeclaration(int offset);

    [CanBeNull]
    FSharpSymbol GetSymbol(int offset);

    [NotNull]
    IReadOnlyList<FcsResolvedSymbolUse> GetAllResolvedSymbols(FSharpCheckFileResults checkResults = null);

    [NotNull]
    IReadOnlyList<FcsResolvedSymbolUse> GetAllDeclaredSymbols(FSharpCheckFileResults checkResults = null);

    /// Documents are currently used in F# files parsing for getting line index info.
    /// This property is only set in FSharpElementFactory to override the document for the context source file
    /// while opening chameleon expressions.
    [CanBeNull]
    IDocument StandaloneDocument { get; set; }

    PsiLanguageType LanguageType { get; set; }
  }
}
