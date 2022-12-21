namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public static class ParameterOwnerMemberDeclarationNavigator
  {
    [JetBrains.Annotations.Pure]
    [JetBrains.Annotations.CanBeNull]
    [JetBrains.Annotations.ContractAnnotation("null => null")]
    public static IParameterOwnerMemberDeclaration GetByParameterPattern(IFSharpPattern? param)
    {
      return (IParameterOwnerMemberDeclaration) BindingNavigator.GetByParameterPattern(param)
             ?? (IParameterOwnerMemberDeclaration) MemberDeclarationNavigator.GetByParameterPattern(param)
             ?? (IParameterOwnerMemberDeclaration) ConstructorDeclarationNavigator.GetByParameterPatterns(param);
    }
  }
}
