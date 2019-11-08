using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public static class FSharpReferenceOwnerNavigator
  {
    public static IFSharpReferenceOwner GetByQualifier([CanBeNull] IFSharpReferenceOwner param) =>
      (IFSharpReferenceOwner) ReferenceExprNavigator.GetByQualifier(param as ISynExpr) ??
      ReferenceNameNavigator.GetByQualifier(param as IReferenceName);
  }
}
