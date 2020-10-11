namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpParensUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi.Tree

type ReplaceLambdaWithOperatorFix(warning: LambdaCanBeReplacedWithOperatorWarning) =
    inherit ReplaceWithReferenceExprFixBase(tryGetParentParens warning.Lambda, warning.OpName,
                                            sprintf "Microsoft.FSharp.Core.Operators.%s" warning.OpName)

    let lambda = warning.Lambda
    let op = warning.OpName

    override x.ResolveContext = lambda.RArrow :> _
    override x.Text = sprintf "Replace lambda with '%s'" op

    override x.AdditionalExecute() =
        let prevToken = x.ExprToReplace.GetPreviousToken()
        let nextToken = x.ExprToReplace.GetNextToken()

        if isNotNull prevToken && not (isWhitespace prevToken) then addNodeBefore x.ExprToReplace (Whitespace())
        if isNotNull nextToken && not (isWhitespace nextToken) then addNodeAfter x.ExprToReplace (Whitespace())
