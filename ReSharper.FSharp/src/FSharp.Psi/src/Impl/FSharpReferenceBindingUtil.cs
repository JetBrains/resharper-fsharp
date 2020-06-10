using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Naming;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class FSharpReferenceBindingUtil
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

      return SuggestShortReferenceName(sourceName, treeNode.Language);
    }

    public static string SuggestShortReferenceName(IDeclaredElement declaredElement, PsiLanguageType language) =>
      SuggestShortReferenceName(declaredElement.GetSourceName(), language);

    public static string SuggestShortReferenceName(string sourceName, PsiLanguageType language) =>
      NamingManager.GetNamingLanguageService(language).MangleNameIfNecessary(sourceName);

    public static void SetRequiredQualifiers([NotNull] FSharpSymbolReference reference,
      [NotNull] IClrDeclaredElement declaredElement)
    {
      var containingType = declaredElement.GetContainingType();
      if (containingType == null)
        return;

      if (containingType.IsModule() && !containingType.RequiresQualifiedAccess())
        return;

      reference.SetQualifier(containingType);
    }
  }
}
