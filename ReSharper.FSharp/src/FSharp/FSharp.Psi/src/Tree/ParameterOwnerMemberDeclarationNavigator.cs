using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public static class ParameterOwnerMemberDeclarationNavigator
  {
    [Pure]
    [CanBeNull]
    [ContractAnnotation("null => null")]
    public static IParameterOwnerMemberDeclaration GetByReferenceParameterPattern([CanBeNull] IReferencePat pat)
    {
      IFSharpPattern param = pat;

      var optionalValPat = OptionalValPatNavigator.GetByPattern(pat);
      param = optionalValPat ?? param;

      var typedPat = TypedPatNavigator.GetByPattern(param);
      param = typedPat ?? param;

      var attributedPat = AttribPatNavigator.GetByPattern(param);
      param = attributedPat ?? param;

      param = param.IgnoreParentParens();

      var parameterOwner = GetByParameterPattern(param);
      if (parameterOwner != null) return parameterOwner;

      param = TuplePatNavigator.GetByPattern(param).IgnoreParentParens();
      return GetByParameterPattern(param);
    }

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
