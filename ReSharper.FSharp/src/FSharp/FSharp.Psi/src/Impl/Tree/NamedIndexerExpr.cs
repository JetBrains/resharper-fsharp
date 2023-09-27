using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NamedIndexerExpr
  {
    public TreeNodeCollection<IFSharpExpression> Args =>
      IndexerExprUtil.GetIndexerArgs(Arg);
  }

  internal partial class ItemIndexerExpr
  {
    public TreeNodeCollection<IFSharpExpression> Args =>
      IndexerExprUtil.GetIndexerArgs(IndexerArgList?.Arg);
  }

  public static class IndexerExprUtil
  {
    public static TreeNodeCollection<IFSharpExpression> GetIndexerArgs([CanBeNull] IFSharpExpression args)
    {
      if (args == null)
        return TreeNodeCollection<IFSharpExpression>.Empty;

      return args is ITupleExpr tupleExpr
        ? tupleExpr.Expressions
        : new TreeNodeCollection<IFSharpExpression>(new[] {args});
    }
  }
}
