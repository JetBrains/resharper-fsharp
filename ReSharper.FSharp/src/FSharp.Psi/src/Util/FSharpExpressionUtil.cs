using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class FSharpExpressionUtil
  {
    private static readonly NodeTypeSet SimpleValueExpressionNodeTypes =
      new NodeTypeSet(
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
      fsExpr.IgnoreInnerParens() is { } expr && SimpleValueExpressionNodeTypes[expr.NodeType];

    public static bool IsLiteralExpression([CanBeNull] this IFSharpExpression fsExpr) =>
      fsExpr.IgnoreInnerParens() is ILiteralExpr literalExpr && literalExpr.IsConstantValue();

    public static readonly Func<IFSharpExpression, bool> IsSimpleValueExpressionFunc = IsSimpleValueExpression;
    public static readonly Func<IFSharpExpression, bool> IsLiteralExpressionFunc = IsLiteralExpression;

    // TODO: change name
    public static IFSharpExpression GetOutermostParentExpressionFromItsReturn(this IFSharpExpression expression)
    {
      var currentExpr = expression;
      while (true)
      {
        if (MatchClauseListOwnerExprNavigator.GetByClauseExpression(currentExpr) is { } matchExpr)
        {
          currentExpr = matchExpr as IFSharpExpression;
          continue;
        }
        
        if (SequentialExprNavigator.GetByExpression(currentExpr) is { } seqExpr &&
            seqExpr.ExpressionsEnumerable.Last() == currentExpr)
        {
          currentExpr = seqExpr;
          continue;
        }

        if ((IfThenElseExprNavigator.GetByBranchExpression(currentExpr)) is {ElseExpr: { }} ifExpr)
        {
          currentExpr = ifExpr;
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
