using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public partial class SequentialExprNavigator
{
    [CanBeNull]
    public static ISequentialExpr GetByLastExpression([CanBeNull] IFSharpExpression expr)
    {
        var seqExpr = GetByExpression(expr);
        var lastExpr = seqExpr?.ExpressionsEnumerable.LastOrDefault();
        return lastExpr != null && lastExpr == expr ? seqExpr : null;
    }

    [CanBeNull]
    public static ISequentialExpr GetByNonLastExpression([CanBeNull] IFSharpExpression expr)
    {
        var seqExpr = GetByExpression(expr);
        var lastExpr = seqExpr?.ExpressionsEnumerable.LastOrDefault();
        return lastExpr != null && lastExpr != expr ? seqExpr : null;
    }
}
