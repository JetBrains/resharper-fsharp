using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class FSharpExpressionUtil
  {
    private static readonly NodeTypeSet ourSimpleValueExpressionNodeTypes =
      new(
        ElementType.LITERAL_EXPR,
        ElementType.UNIT_EXPR,
        ElementType.ARRAY_EXPR,
        ElementType.LIST_EXPR,
        ElementType.QUOTE_EXPR,
        ElementType.OBJ_EXPR,
        ElementType.NEW_EXPR,
        ElementType.RECORD_EXPR,
        ElementType.ANON_RECORD_EXPR,
        ElementType.DO_EXPR,
        ElementType.LAZY_EXPR,
        ElementType.TYPE_TEST_EXPR,
        ElementType.FOR_EXPR,
        ElementType.FOR_EACH_EXPR,
        ElementType.WHILE_EXPR);

    public static bool IsSimpleValueExpression([CanBeNull] this IFSharpExpression fsExpr) =>
      fsExpr.IgnoreInnerParens() is { } expr && ourSimpleValueExpressionNodeTypes[expr.NodeType];

    public static bool IsLiteralExpression([CanBeNull] this IFSharpExpression fsExpr) =>
      fsExpr.IgnoreInnerParens() is ILiteralExpr literalExpr && literalExpr.IsConstantValue();

    /// Checks exactly for ILambdaExpr
    public static bool IsLambdaExpression([CanBeNull] this IFSharpExpression fsExpr) => fsExpr is ILambdaExpr;

    public static readonly Func<IFSharpExpression, bool> IsSimpleValueExpressionFunc = IsSimpleValueExpression;
    public static readonly Func<IFSharpExpression, bool> IsLiteralExpressionFunc = IsLiteralExpression;
    public static readonly Func<IFSharpExpression, bool> IsLambdaExpressionFunc = IsLambdaExpression;

    // TODO: change name
    [CanBeNull]
    public static IFSharpExpression GetOutermostParentExpressionFromItsReturn([NotNull] this IFSharpExpression expression, bool allowFromLambdaReturn = false)
    {
      var currentExpr = expression;
      while (true)
      {
        currentExpr = currentExpr.IgnoreParentParens();

        if (MatchClauseListOwnerExprNavigator.GetByClauseExpression(currentExpr) is { } matchExpr)
        {
          currentExpr = matchExpr as IFSharpExpression;
          continue;
        }

        if (SequentialExprNavigator.GetByLastExpression(currentExpr) is { } seqExpr)
        {
          currentExpr = seqExpr;
          continue;
        }

        if (IfThenElseExprNavigator.GetByBranchExpression(currentExpr) is { ElseExpr: not null } ifExpr)
        {
          currentExpr = ifExpr;
          continue;
        }

        if (ElifExprNavigator.GetByBranchExpression(currentExpr) is { ElseExpr: not null } elifExpr)
        {
          currentExpr = elifExpr;
          continue;
        }

        if (allowFromLambdaReturn && LambdaExprNavigator.GetByExpression(currentExpr) is LambdaExpr lambdaExpr)
        {
          currentExpr = lambdaExpr;
          continue;
        }

        if (LetOrUseExprNavigator.GetByInExpression(currentExpr) is { } letOrUseExpr)
        {
          currentExpr = letOrUseExpr;
          continue;
        }

        return currentExpr;
      }
    }
  }
}
