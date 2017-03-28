using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  public interface IFSharpFileCheckInfoOwner : ICompositeElement
  {
    [CanBeNull]
    FSharpParseFileResults ParseResults { get; set; }

    [CanBeNull]
    FSharpCheckFileResults GetCheckResults(Action interruptChecker = null);

    [CanBeNull]
    FSharpCheckFileResults PreviousCheckResults { get; set; }

    [CanBeNull]
    Ast.ParsedInput ParseTree { get; }

    /// <summary>
    /// True when SetResolvedSymbolsStageProcess is finished.
    /// </summary>
    bool ReferencesResolved { get; set; }

    bool IsChecked { get; }

    FSharpCheckerService CheckerService { get; set; }
    FSharpProjectOptions ProjectOptions { get; set; }
  }
}