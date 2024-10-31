using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public static class ParameterOwnerMemberDeclarationNavigator
  {
    [Pure]
    [CanBeNull]
    [ContractAnnotation("null => null")]
    public static IParameterOwnerMemberDeclaration GetByParameterPattern([CanBeNull] IFSharpPattern param) =>
      (IParameterOwnerMemberDeclaration)BindingNavigator.GetByParameterPattern(param) ??
      (IParameterOwnerMemberDeclaration)MemberDeclarationNavigator.GetByParameterPattern(param) ??
      (IParameterOwnerMemberDeclaration)ConstructorDeclarationNavigator.GetByParameterPatterns(param);

    [Pure]
    [CanBeNull]
    [ContractAnnotation("null => null")]
    public static IParameterOwnerMemberDeclaration GetByExpression(IFSharpExpression expr) =>
      (IParameterOwnerMemberDeclaration)BindingNavigator.GetByExpression(expr) ??
      (IParameterOwnerMemberDeclaration)MemberDeclarationNavigator.GetByExpression(expr) ??
      SecondaryConstructorDeclarationNavigator.GetByExpression(expr);
  }
}
