using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  public interface IFSharpFileCheckInfoOwner : ICompositeElement
  {
    [CanBeNull]
    FSharpOption<FSharpParseAndCheckResults> GetParseAndCheckResults(Action interruptChecker = null,
      bool allowStaleResults = false);

    /// <summary>
    /// True when SetResolvedSymbolsStageProcess is finished.
    /// </summary>
    bool ReferencesResolved { get; set; }

    FSharpCheckerService CheckerService { get; set; }
    FSharpProjectOptions ProjectOptions { get; set; }

    TokenBuffer ActualTokenBuffer { get; set; }
    
    [CanBeNull]
    FSharpOption<FSharpParseFileResults> ParseResults { get; set; }

    [CanBeNull]
    FSharpSymbol GetSymbolDeclaration(int offset);
    
  }
}