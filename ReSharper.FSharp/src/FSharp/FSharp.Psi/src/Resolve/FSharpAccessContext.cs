using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;

public class FSharpAccessContext([NotNull] ITreeNode element)
  : ElementAccessContext(element), ILanguageSpecificAccessContext
{
  public ITreeNode TreeNode => Element;

  bool ILanguageSpecificAccessContext.IsAccessible(ILanguageSpecificDeclaredElement declaredElement) =>
    declaredElement is IFSharpDeclaredElement;
}
