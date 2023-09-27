using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public static class FSharpReferenceOwnerNavigator
  {
    public static IFSharpReferenceOwner GetByQualifier([CanBeNull] IFSharpReferenceOwner param) =>
      ReferenceExprNavigator.GetByQualifier(param as IFSharpExpression) ??
      (IFSharpReferenceOwner) ReferenceNameNavigator.GetByQualifier(param as IReferenceName) ??
      TypeExtensionDeclarationNavigator.GetByQualifierReferenceName(param as IReferenceName);

    public static IFSharpReferenceOwner GetByIdentifier([CanBeNull] IFSharpIdentifier identifier) =>
      ReferenceExprNavigator.GetByIdentifier(identifier) ??
      (IFSharpReferenceOwner) ReferenceNameNavigator.GetByIdentifier(identifier) ??
      TypeExtensionDeclarationNavigator.GetByIdentifier(identifier);
  }
}
