using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public static class FSharpTypeOwnerDeclarationNavigator
{
  [CanBeNull]
  public static IFSharpTypeOwnerDeclaration GetByExpression([CanBeNull] IFSharpExpression expr) =>
    (IFSharpTypeOwnerDeclaration)BindingNavigator.GetByExpression(expr) ??
    (IFSharpTypeOwnerDeclaration)MemberDeclarationNavigator.GetByExpression(expr) ??
    (IFSharpTypeOwnerDeclaration)AutoPropertyDeclarationNavigator.GetByExpression(expr);

  [CanBeNull] public static IFSharpTypeOwnerDeclaration GetByTypeUsage([CanBeNull] ITypeUsage typeUsage) =>
    (IFSharpTypeOwnerDeclaration)MemberSignatureLikeDeclarationNavigator.GetByTypeUsage(typeUsage) ??
    (IFSharpTypeOwnerDeclaration)BindingSignatureNavigator.GetByTypeUsage(typeUsage);
}
