using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class ModuleSuffixUtil
  {
    [CanBeNull]
    public static INestedModuleDeclaration GetAssociatedModuleDeclaration(
      [NotNull] this IFSharpTypeOldDeclaration typeDeclaration)
    {
      if (!(typeDeclaration.Parent is IModuleLikeDeclaration parentModule))
        return null;

      foreach (var moduleDeclaration in parentModule.Children<INestedModuleDeclaration>())
        if (moduleDeclaration.GetAssociatedTypeDeclaration(out _) == typeDeclaration)
          return moduleDeclaration;

      return null;
    }

    [CanBeNull]
    public static ITypeElement GetAssociatedModule([NotNull] this ITypeElement typeElement)
    {
      foreach (var declaration in typeElement.GetDeclarations())
      {
        if (!(declaration is IFSharpTypeOldDeclaration typeDeclaration))
          continue;

        var moduleDeclaration = typeDeclaration.GetAssociatedModuleDeclaration();
        if (moduleDeclaration != null)
          return ((ITypeDeclaration) moduleDeclaration).DeclaredElement;
      }

      return null;
    }
  }
}
