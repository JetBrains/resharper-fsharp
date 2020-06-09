using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public static class FSharpTreeNodeExtensions
  {
    [NotNull]
    public static IFSharpLanguageService GetFSharpLanguageService([NotNull] this IFSharpTreeNode treeNode) =>
      treeNode.GetPsiServices().GetComponent<LanguageManager>().GetService<IFSharpLanguageService>(treeNode.Language);

    [NotNull]
    public static IFSharpElementFactory CreateElementFactory([NotNull] this IFSharpTreeNode treeNode) =>
      treeNode.GetFSharpLanguageService().CreateElementFactory(treeNode.GetPsiModule());

    [NotNull]
    public static FSharpCheckerService GetFcsCheckerService([NotNull] this IFSharpTreeNode treeNode) =>
      treeNode.FSharpFile.CheckerService;
  }
}
