using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public static class FSharpTypeOwnerDeclarationNavigator
{
  [CanBeNull]
  public static IFSharpTypeOwnerDeclaration GetByExpression([NotNull] IFSharpExpression expr) =>
    (IFSharpTypeOwnerDeclaration)BindingNavigator.GetByExpression(expr) ??
    (IFSharpTypeOwnerDeclaration)MemberDeclarationNavigator.GetByExpression(expr) ??
    (IFSharpTypeOwnerDeclaration)AutoPropertyDeclarationNavigator.GetByExpression(expr);
}
