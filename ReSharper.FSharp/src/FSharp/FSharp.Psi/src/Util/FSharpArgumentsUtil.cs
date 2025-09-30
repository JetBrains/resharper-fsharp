using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util;

public static class FSharpArgumentsUtil
{
  /// Has the 'name = expr' form
  public static bool HasNamedArgStructure(IBinaryAppExpr app) =>
    TryGetNamedArgRefExpr(app) != null;

  public static IReferenceExpr TryGetNamedArgRefExpr(IFSharpExpression expr) =>
    expr is IBinaryAppExpr { ShortName: "=", LeftArgument: IReferenceExpr { IsSimpleName: true } refExpr }
      ? refExpr
      : null;

  public static bool IsTopLevelArg(IFSharpExpression expr)
  {
    var tupleExpr = TupleExprNavigator.GetByExpression(expr);
    var argExpr = tupleExpr ?? expr;

    var parenExpr = ParenExprNavigator.GetByInnerExpression(argExpr);
    var argOwner = FSharpArgumentOwnerNavigator.GetByArgumentExpression(parenExpr);

    return argOwner != null;
  }

  /// IBinaryAppExpr used exactly as a named argument (without taking into account resolve)
  public static bool IsNamedArgSyntactically(IBinaryAppExpr app) =>
    HasNamedArgStructure(app) && IsTopLevelArg(app);
}
