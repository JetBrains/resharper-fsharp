using FSharp.Compiler;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IChameleonExpression
  {
    Ast.SynExpr SynExpr { get; }
  }
}
