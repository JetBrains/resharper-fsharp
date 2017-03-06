using JetBrains.Annotations;

namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  public static class FSharpTreeNodeExtensions
  {
    [CanBeNull, Pure]
    public static IFSharpNamespaceDeclaration GetContainingNamespaceDeclaration([NotNull] this IFSharpTreeNode treeNode)
    {
      return treeNode.GetContainingNode<IFSharpNamespaceDeclaration>();
    }

    [CanBeNull, Pure]
    public static IFSharpTypeDeclaration GetContainingTypeDeclaration([NotNull] this IFSharpTreeNode treeNode)
    {
      return treeNode.GetContainingNode<IFSharpTypeDeclaration>();
    }
  }
}