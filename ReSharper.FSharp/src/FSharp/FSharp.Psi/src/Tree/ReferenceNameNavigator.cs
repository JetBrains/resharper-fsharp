using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class ReferenceNameNavigator
  {
    public static IReferenceName GetByQualifier([CanBeNull] IReferenceName param) =>
      (IReferenceName) ExpressionReferenceNameNavigator.GetByQualifier(param as IExpressionReferenceName) ??
      TypeReferenceNameNavigator.GetByQualifier(param as ITypeReferenceName);
  }
}
