using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
{
  public static class DocCommentBlockNodeNavigator
  {
    [CanBeNull]
    public static IFSharpDocCommentBlock GetByDocCommentNode([NotNull] ICommentNode docCommentNode) =>
      docCommentNode.Parent as IFSharpDocCommentBlock;
  }
}
