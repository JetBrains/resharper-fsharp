using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public static class FSharpSandBoxUtil
  {
    public static TNode TryGetOriginalNodeThroughSandBox<TNode>([NotNull] this TNode sandboxNode)
      where TNode : class, IFSharpTreeNode
    {
      // todo: simplify getting the node from the original file
      // ParentThroughSandBox doesn't work due to ContextType == Replace,
      // it tries to get Parent of the whole original file.

      if (sandboxNode.GetContainingFile()?.Parent is not ISandBox { ContextNode: IFSharpFile fsFile })
        return null;

      var sandboxNodeRange = sandboxNode.GetTreeTextRange();
      var sandboxNodeRangePlusOne = sandboxNode.GetTreeTextRange().ExtendRight(1);

      var token = fsFile.FindTokenAt(sandboxNode.GetTreeEndOffset() - 1);
      foreach (var treeNode in token.ContainingNodes<TNode>(true))
      {
        var nodeRange = treeNode.GetTreeTextRange();
        if (nodeRange == sandboxNodeRange || nodeRange == sandboxNodeRangePlusOne)
          return treeNode;

        if (nodeRange.Length > sandboxNodeRange.Length)
          return null;
      }

      return null;
    }

    public static IFSharpExpression TryGetOriginalRecordExprThroughSandBox([NotNull] this IFSharpExpression sandboxNode)
    {
      if (sandboxNode.GetContainingFile()?.Parent is not ISandBox { ContextNode: IFSharpFile fsFile })
        return null;

      var sandboxNodeStartOffset = sandboxNode.GetTreeStartOffset();
      var token = fsFile.FindTokenAt(sandboxNodeStartOffset);

      foreach (var treeNode in token.ContainingNodes<IFSharpExpression>(true))
      {
        var nodeStartOffset = treeNode.GetTreeStartOffset();
        if (nodeStartOffset == sandboxNodeStartOffset && treeNode is IRecordLikeExpr || treeNode is IComputationExpr)
          return treeNode;

        if (nodeStartOffset < sandboxNodeStartOffset)
          return null;
      }

      return null;
    }
  }
}
