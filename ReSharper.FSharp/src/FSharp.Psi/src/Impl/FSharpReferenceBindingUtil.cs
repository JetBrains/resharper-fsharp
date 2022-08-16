using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Naming;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
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

    [CanBeNull]
    private static INamespace GetNamespace([NotNull] IClrDeclaredElement declaredElement)
    {
      if (declaredElement is ITypeElement typeElement)
        return typeElement.GetContainingNamespace();

      if (declaredElement.GetContainingType() is { } containingType)
        return containingType.GetContainingNamespace();

      if (declaredElement is INamespace ns)
        return ns.GetContainingNamespace();

      return null;
    }

    private static bool RequiresFullyQualifiedName([NotNull] IReference reference)
    {
      // Can't insert 'open' before top-level module declaration.
      var attribute = reference.GetTreeNode().GetContainingNode<IAttribute>(true);
      return NamedModuleDeclarationNavigator.GetByAttribute(attribute) != null;
    }

    private static void SetNamespaceQualifierIfNeeded([NotNull] FSharpSymbolReference reference,
      [NotNull] IClrDeclaredElement declaredElement)
    {
      if (!RequiresFullyQualifiedName(reference))
        return;

      var ns = GetNamespace(declaredElement);
      if (ns == null || ns.IsRootNamespace)
        return;

      reference.SetQualifier(ns);
    }

    /// Doesn't take opened modules into account, only checks if qualifiers are required by containing type or syntax.
    public static void SetRequiredQualifiers([NotNull] FSharpSymbolReference reference,
      [NotNull] IClrDeclaredElement declaredElement)
    {
      var containingType = declaredElement.GetContainingType();
      if (containingType == null)
      {
        SetNamespaceQualifierIfNeeded(reference, declaredElement);
        return;
      }

      if (containingType.IsModule() && !containingType.RequiresQualifiedAccess())
      {
        SetNamespaceQualifierIfNeeded(reference, containingType);
        return;
      }

      if ((containingType.IsUnion() || containingType.IsRecord()) && !containingType.RequiresQualifiedAccess())
      {
        // Containing module may require qualifier.
        SetRequiredQualifiers(reference, containingType);
        return;
      }

      var containingTypeDecls = reference.GetElement().ContainingNodes<ITypeDeclaration>();
      foreach (var typeDecl in containingTypeDecls)
        if (containingType.Equals(typeDecl.DeclaredElement))
          return;

      reference.SetQualifier(containingType);
    }
  }
}
