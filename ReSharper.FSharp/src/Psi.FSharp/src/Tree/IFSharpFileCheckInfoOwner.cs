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
    FSharpOption<FSharpParseFileResults> GetParseResults(bool keepResults = false, Action interruptChecker = null);
    
    [CanBeNull]
    FSharpCheckFileResults GetCheckResults(bool forceRecheck = false, Action interruptChecker = null);

    /// <summary>
    /// True when SetResolvedSymbolsStageProcess is finished.
    /// </summary>
    bool ReferencesResolved { get; set; }

    bool IsChecked { get; }

    FSharpCheckerService CheckerService { get; set; }
    FSharpProjectOptions ProjectOptions { get; set; }

    TokenBuffer ActualTokenBuffer { get; set; }
  }
}