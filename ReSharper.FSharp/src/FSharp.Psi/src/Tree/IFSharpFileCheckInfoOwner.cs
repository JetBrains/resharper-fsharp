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

    [NotNull] FSharpCheckerService FcsCheckerService { get; set; }

    [NotNull] IFSharpResolvedSymbolsCache ResolvedSymbolsCache { get; set; }

    [CanBeNull] FSharpOption<FSharpParseFileResults> ParseResults { get; set; }

    [CanBeNull] FSharpOption<ParsedInput> ParseTree { get; }

    [CanBeNull]
    FSharpSymbolUse GetSymbolUse(int offset);

    [CanBeNull]
    FSharpSymbolUse GetSymbolDeclaration(int offset);

    [CanBeNull]
    FSharpSymbol GetSymbol(int offset);

    [NotNull]
    IReadOnlyList<FSharpResolvedSymbolUse> GetAllResolvedSymbols(FSharpCheckFileResults checkResults = null);

    [NotNull]
    IReadOnlyList<FSharpResolvedSymbolUse> GetAllDeclaredSymbols(FSharpCheckFileResults checkResults = null);

    /// Documents are currently used in F# files parsing for getting line index info.
    /// This property is only set in FSharpElementFactory to make the document accessible without having source file
    /// while opening chameleon expressions.
    [CanBeNull]
    IDocument StandaloneDocument { get; set; }
    
    PsiLanguageType LanguageType { get; set; }
  }
}
