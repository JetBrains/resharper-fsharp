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
    public static IFSharpExpression GetOuterMostParentExpression(this IFSharpExpression expression)
    {
      IFSharpTreeNode currentExpr = expression;
      var parent = currentExpr.Parent;
      while (parent != null)
      {
        switch (parent)
        {
          case IChameleonExpression:
            return currentExpr as IFSharpExpression;
          case ILiteralExpr literalExpr:
            currentExpr = literalExpr;
            parent = literalExpr.Parent;
            break;
          case IMatchClause matchClause:
            if (matchClause.Expression == currentExpr)
            {
              currentExpr = matchClause;
              parent = matchClause.Parent;
            }
            else
            {
              currentExpr = null;
              parent = null;
            }
            break;
          case ISequentialExpr sequentialExpr:
            if (sequentialExpr.Expressions.Last() == currentExpr)
            {
              currentExpr = sequentialExpr;
              parent = sequentialExpr.Parent;
            }
            else
            {
              currentExpr = null;
              parent = null;
            }
            break;
          case IIfThenElseExpr ifThenElseExpr:
            if (ifThenElseExpr.ThenExpr == currentExpr || ifThenElseExpr.ElseExpr == currentExpr)
            {
              currentExpr = ifThenElseExpr;
              parent = ifThenElseExpr.Parent;
            }
            else
            {
              currentExpr = null;
              parent = null;
            }
            break;
          case IForExpr forExpr:
            if (forExpr.DoExpression == currentExpr)
            {
              currentExpr = forExpr;
              parent = forExpr.Parent;
            }
            else
            {
              currentExpr = null;
              parent = null;
            }
            break;
          case ILetOrUseExpr letOrUseExpr:
            if (letOrUseExpr.InExpression == currentExpr)
            {
              currentExpr = letOrUseExpr;
              parent = letOrUseExpr.Parent;
            }
            else
            {
              currentExpr = null;
              parent = null;
            }
            break;
          case IWhileExpr whileExpr:
            if (whileExpr.DoExpression == currentExpr)
            {
              currentExpr = whileExpr;
              parent = whileExpr.Parent;
            }
            else
            {
              currentExpr = null;
              parent = null;
            }
            break;
          case IFSharpExpression fSharpExpression:
            currentExpr = fSharpExpression;
            parent = fSharpExpression.Parent;
            break;
          default:
            currentExpr = null;
            parent = null;
            break;
        }
      }

      return currentExpr as IFSharpExpression;
    }
  }
}
