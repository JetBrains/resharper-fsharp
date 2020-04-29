using FSharp.Compiler;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IChameleonExpression
  {
    SyntaxTree.SynExpr SynExpr { get; }
    int OriginalStartOffset { get; }
    int OriginalLineStart { get; }
  }
}
