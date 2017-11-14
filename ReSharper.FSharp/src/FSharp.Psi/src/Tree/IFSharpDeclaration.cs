using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IFSharpDeclaration : IDeclaration
  {
    /// <summary>
    /// May take long time due to waiting for FCS. Symbol is cached in declaration.
    /// </summary>
    [CanBeNull]
    FSharpSymbol GetFSharpSymbol();

    [NotNull]
    string ShortName { get; }

    [NotNull]
    string SourceName { get; }
  }
}