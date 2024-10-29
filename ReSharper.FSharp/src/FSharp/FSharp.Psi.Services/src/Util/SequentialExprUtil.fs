module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.SequentialExprUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

let isLastExprInSeqExpr (seqExpr: ISequentialExpr) (expr: IFSharpExpression) =
    isNotNull seqExpr && seqExpr.ExpressionsEnumerable.LastOrDefault() == expr

let isLastExpr (expr: IFSharpExpression) =
    let seqExpr = SequentialExprNavigator.GetByExpression(expr)
    isLastExprInSeqExpr seqExpr expr
