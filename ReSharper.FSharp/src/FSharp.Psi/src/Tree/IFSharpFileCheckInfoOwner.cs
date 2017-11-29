using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IFSharpFileCheckInfoOwner : ICompositeElement
  {
    [CanBeNull]
    FSharpOption<FSharpParseAndCheckResults> GetParseAndCheckResults(bool allowStaleResults,
      Action interruptChecker = null);

    FSharpOption<FSharpParsingOptions> ParsingOptions { get; set; }

    FSharpCheckerService CheckerService { get; set; }

    TokenBuffer ActualTokenBuffer { get; set; }

    [CanBeNull]
    FSharpOption<FSharpParseFileResults> ParseResults { get; set; }

    [CanBeNull]
    FSharpSymbol GetSymbolUse(int offset);

    [CanBeNull]
    FSharpSymbol GetSymbolDeclaration(int offset);

    [NotNull]
    FSharpResolvedSymbolUse[] GetAllResolvedSymbols(FSharpCheckFileResults checkResults = null, Action interruptChecker = null);

    [NotNull]
    FSharpResolvedSymbolUse[] GetAllDeclaredSymbols(FSharpCheckFileResults checkResults = null, Action interruptChecker = null);
  }
}