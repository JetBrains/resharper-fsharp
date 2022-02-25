using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
{
  public class DocCommentBlockNodeNavigator
  {
    [CanBeNull]
    public static IFSharpDocCommentBlock GetByDocCommentNode([NotNull] IFSharpDocCommentNode docCommentNode)
    {
      return docCommentNode.Parent as IFSharpDocCommentBlock;
    }
  }
}
