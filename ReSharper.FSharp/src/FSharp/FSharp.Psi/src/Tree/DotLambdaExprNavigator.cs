using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public partial class DotLambdaExprNavigator
{
  [CanBeNull]
  public static IDotLambdaExpr GetByFirstQualifier([CanBeNull] IFSharpExpression param) =>
    param is IReferenceExpr { Qualifier: null } refExpr
      ? GetByQualifier(refExpr)
      : null;

  [CanBeNull]
  public static IDotLambdaExpr GetByQualifier([CanBeNull] IFSharpExpression expr)
  {
    while (GetQualifiedExpr(expr) is { } qualifiedExpr)
      expr = qualifiedExpr;

    return GetByExpression(expr);
  }

  [CanBeNull]
  private static IFSharpExpression GetQualifiedExpr([CanBeNull] IFSharpExpression expr)
  {
    if (QualifiedExprNavigator.GetByQualifier(expr) is { } qualifiedExpr)
      return qualifiedExpr;

    if (PrefixAppExprNavigator.GetByFunctionExpression(expr) is { IsHighPrecedence: true } appExpr)
      return appExpr;

    return null;
  }
}
