using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public static class FSharpTreeNodeExtensions
  {
    [CanBeNull, Pure]
    public static INamedNamespaceDeclaration GetContainingNamespaceDeclaration(this IFSharpTreeNode treeNode)
    {
      return treeNode?.GetContainingNode<INamedNamespaceDeclaration>();
    }

    [CanBeNull, Pure]
    public static IFSharpTypeElementDeclaration GetContainingTypeDeclaration([NotNull] this ITreeNode treeNode)
    {
      while (treeNode != null && !(treeNode is ITypeDeclaration))
        treeNode = treeNode.Parent;

      return (IFSharpTypeElementDeclaration) treeNode;
    }
  }
}
