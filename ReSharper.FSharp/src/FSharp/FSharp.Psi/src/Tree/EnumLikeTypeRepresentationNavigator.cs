namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class EnumLikeTypeRepresentationNavigator
  {
    public static IEnumLikeTypeRepresentation GetByEnumLikeCase(IEnumCaseLikeDeclaration param) =>
      (IEnumLikeTypeRepresentation)UnionRepresentationNavigator.GetByUnionCase(param as IUnionCaseDeclaration) ??
      EnumRepresentationNavigator.GetByEnumCase(param as IEnumCaseDeclaration);
  }
}
