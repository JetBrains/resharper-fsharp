using System;
using JetBrains.Annotations;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  public interface IFSharpFileCheckInfoOwner
  {
    [CanBeNull]
    FSharpParseFileResults ParseResults { get; set; }

    [CanBeNull]
    FSharpCheckFileResults GetCheckResults(Action interruptChecker = null);

    /// <summary>
    /// True when SetResolvedSymbolsStageProcess is finished.
    /// </summary>
    bool ReferencesResolved { get; set; }

    bool IsChecked { get; }
  }
}