using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  public partial interface IFSharpDeclaration : IDeclaration
  {
    FSharpSymbol Symbol { get; set; }
  }
}