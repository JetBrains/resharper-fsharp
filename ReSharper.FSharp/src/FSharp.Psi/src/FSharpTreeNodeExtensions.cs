using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public static class FSharpTreeNodeExtensions
  {
    [NotNull]
    public static IFSharpLanguageService GetFSharpLanguageService([NotNull] this ITreeNode treeNode) =>
      treeNode.GetPsiServices().GetComponent<LanguageManager>().GetService<IFSharpLanguageService>(treeNode.Language);

    [NotNull]
    public static IFSharpElementFactory CreateElementFactory([NotNull] this ITreeNode treeNode, string extension = null) =>
      treeNode.GetFSharpLanguageService().CreateElementFactory(treeNode.GetSourceFile(), treeNode.GetPsiModule(), extension);

    public static bool IsFSharpSigFile([NotNull] this ITreeNode treeNode) =>
      treeNode.GetContainingFile() is IFSharpSigFile;
  }
}
