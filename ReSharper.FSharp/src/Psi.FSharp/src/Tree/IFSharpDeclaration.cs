using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  public partial interface IFSharpDeclaration : IDeclaration
  {
    /// <summary>
    /// May take long time due to waiting for FCS. Symbol is cached in declaration.
    /// </summary>
    [CanBeNull]
    FSharpSymbol GetFSharpSymbol();

    [CanBeNull]
    FSharpSymbol Symbol { get; set; }

    [NotNull]
    string ShortName { get; }

    [NotNull]
    string SourceName { get; }
  }
}