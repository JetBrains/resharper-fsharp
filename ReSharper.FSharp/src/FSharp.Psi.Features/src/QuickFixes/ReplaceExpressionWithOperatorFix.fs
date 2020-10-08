namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpLambdaUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

type ReplaceExpressionWithOperatorFix(expr: IFSharpExpression, op: string, opFullName: string) =
    inherit FSharpQuickFixBase()

    let replaceLambdaBody (lambda: ILambdaExpr) = op = "id" && not (lambda.PatternsEnumerable.CountIs(1))

    let idIsShadowed =
        match expr with
        | :? ILambdaExpr as lambda ->
            match lambda.CheckerService.ResolveNameAtLocation(lambda.RArrow, [op], "ReplaceExpressionWithOperatorFix") with
            | Some symbolUse -> symbolUse.Symbol.FullName <> opFullName
            | None -> false
        | _ -> false

    override x.Text =
        match expr with
        | :? ILambdaExpr as lambda ->
            if replaceLambdaBody lambda then "Replace lambda body with 'id'"
            else sprintf "Replace lambda with '%s'" op
        | _ -> sprintf "Replace with '%s'" op

    new (warning: ExpressionCanBeReplacedWithIdWarning) =
        ReplaceExpressionWithOperatorFix(warning.Expr, "id", "Microsoft.FSharp.Core.Operators.id")

    new (warning: ExpressionCanBeReplacedWithFstWarning) =
        ReplaceExpressionWithOperatorFix(warning.Expr, "fst", "Microsoft.FSharp.Core.Operators.fst")
    
    new (warning: ExpressionCanBeReplacedWithSndWarning) =
        ReplaceExpressionWithOperatorFix(warning.Expr, "snd", "Microsoft.FSharp.Core.Operators.snd")

    override x.IsAvailable _ =
        let isApplicable =
            match expr with
            | :? ILambdaExpr as lambda -> isValid lambda
            | _ -> false

        isApplicable && not idIsShadowed

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let factory = expr.CreateElementFactory()

        match expr with
        | :? ILambdaExpr as lambda ->
            let pats = lambda.Patterns
            let replaceLambda = not (replaceLambdaBody lambda)

            if replaceLambda then
                let paren = ParenExprNavigator.GetByInnerExpression(lambda)
                let nodeToReplace = if isNotNull paren then paren :> IFSharpExpression else expr

                let prevToken = nodeToReplace.GetPreviousToken()
                let nextToken = nodeToReplace.GetNextToken()

                if isNotNull prevToken && not (isWhitespace prevToken) then addNodeBefore nodeToReplace (Whitespace())
                if isNotNull nextToken && not (isWhitespace nextToken) then addNodeAfter nodeToReplace (Whitespace())

                replace nodeToReplace (factory.CreateReferenceExpr(op))
            else
                deletePatternsFromEnd lambda 1
                replace lambda.Expression (factory.CreateReferenceExpr(op))
        | _ -> ()
