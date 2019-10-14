using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Naming;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class FSharpBindingUtil
  {
    [NotNull]
    public static string SuggestShortReferenceName([NotNull] IReference reference,
      [NotNull] IDeclaredElement declaredElement)
    {
      var sourceName = declaredElement.GetSourceName();

      var treeNode = reference.GetTreeNode();
      if (declaredElement is ITypeElement)
      {
        var referenceName = treeNode as ITypeReferenceName;
        if (AttributeNavigator.GetByReferenceName(referenceName) != null &&
            sourceName.EndsWith(FSharpImplUtil.AttributeSuffix, StringComparison.Ordinal) &&
            sourceName != FSharpImplUtil.AttributeSuffix)
        {
          sourceName = sourceName.TrimFromEnd(FSharpImplUtil.AttributeSuffix);
        }
      }

      return NamingManager.GetNamingLanguageService(treeNode.Language).MangleNameIfNecessary(sourceName);
    }
  }
}
