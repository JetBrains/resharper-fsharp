using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public static class FSharpSandBoxUtil
  {
    public static TNode TryGetOriginalNodeThroughSandBox<TNode>([NotNull] this IReferenceExpr initialRefExpr, 
      [NotNull] TNode wholeNode)
      where TNode : class, IFSharpExpression
    {
      // todo: simplify getting the node from the original file
      // ParentThroughSandBox doesn't work due to ContextType == Replace,
      // it tries to get Parent of the whole original file.

      // x.{caret}
      // x.le{caret}
      // f x.{caret}
      // f x.le{caret}

      if (initialRefExpr.GetContainingFile()?.Parent is not ISandBox { ContextNode: IFSharpFile fsFile })
        return null;

      var token = fsFile.FindTokenAt(initialRefExpr.Delimiter.GetTreeStartOffset());
      if (token == null) return null;

      var range = new TreeTextRange(wholeNode.GetTreeStartOffset(), token.Parent.NotNull().GetTreeEndOffset());
      foreach (var treeNode in token.ContainingNodes<TNode>(true))
      {
        var nodeRange = treeNode.GetTreeTextRange();
        if (nodeRange == range)
          return treeNode;

        if (nodeRange.Length > range.Length)
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

    public static TNode TryGetOriginalNodeThroughSandBox<TNode>([NotNull] this TNode sandboxNode)
      where TNode : class, ITreeNode
    {
      if (sandboxNode.GetContainingFile()?.Parent is not ISandBox { ContextNode: IFSharpFile fsFile })
        return null;

      var sandboxNodeStartOffset = sandboxNode.GetTreeStartOffset();
      var token = fsFile.FindTokenAt(sandboxNodeStartOffset);

      foreach (var treeNode in token.ContainingNodes<TNode>(true))
      {
        var nodeStartOffset = treeNode.GetTreeStartOffset();
        if (nodeStartOffset == sandboxNodeStartOffset)
          return treeNode;

        if (nodeStartOffset < sandboxNodeStartOffset)
          return null;
      }

      return null;
    }
  }
}
