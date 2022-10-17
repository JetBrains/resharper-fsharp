namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public static class ParameterOwnerMemberDeclarationNavigator
  {
    [Annotations.Pure]
    [Annotations.CanBeNull]
    [Annotations.ContractAnnotation("null => null")]
    public static IParameterOwnerMemberDeclaration GetByParameterPattern(IFSharpPattern? param)
    {
      return (IParameterOwnerMemberDeclaration) BindingNavigator.GetByParameterPattern(param)
             ?? (IParameterOwnerMemberDeclaration) MemberDeclarationNavigator.GetByParameterPattern(param)
             ?? (IParameterOwnerMemberDeclaration) ConstructorDeclarationNavigator.GetByParameterPatterns(param);
    }
  }
}
