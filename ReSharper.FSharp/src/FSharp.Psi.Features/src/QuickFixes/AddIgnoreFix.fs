namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type AddIgnoreFix(expr: IFSharpExpression) =
    inherit FSharpQuickFixBase()

    let mutable expr = expr

    let shouldAddNewLine (expr: IFSharpExpression) =
        if expr.IsSingleLine then false else
        if lastBlockHasSameIndent expr then false else

        match expr with
        | :? IMatchLikeExpr | :? IIfThenElseExpr | :? ITryLikeExpr | :? ILambdaExpr
        | :? IDoExpr | :? IAssertExpr | :? ILazyExpr -> true
        | _ -> false

    let suggestInnerExpression (expr: IFSharpExpression) =
        match expr with
        | :? IIfThenElseExpr as ifExpr ->
            Some(ifExpr.ThenExpr, "Then branch")

        | :? ITryLikeExpr as tryExpr ->
            Some(tryExpr.TryExpression, "Try branch")

        | :? IMatchLikeExpr as matchExpr ->
            let clauses = matchExpr.Clauses
            if clauses.Count <= 1 then None else

            Some(clauses.[0].Expression, "First clause")

        | _ -> None

    new (warning: UnitTypeExpectedWarning) =
        AddIgnoreFix(warning.Expr)

    new (warning: FunctionValueUnexpectedWarning) =
        AddIgnoreFix(warning.Expr)

    new (error: UnitTypeExpectedError) =
        AddIgnoreFix(error.Expr)

    override x.Text = "Ignore value"
    override x.IsAvailable _ = isValid expr

    override x.Execute(solution, textControl) =
        expr <-
            match suggestInnerExpression expr with
            | Some(innerExpression, text) when isNotNull innerExpression ->
                let occurrences =
                    [| innerExpression, text
                       expr, "Whole expression" |]
                x.SelectExpression(occurrences, solution, textControl)
            | _ -> expr

        if isNotNull expr then
            base.Execute(solution, textControl)

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        
        let ignoreApp = expr.CreateElementFactory().CreateIgnoreApp(expr, shouldAddNewLine expr)

        let replaced = ModificationUtil.ReplaceChild(expr, ignoreApp).As<IBinaryAppExpr>()
        addParensIfNeeded replaced.LeftArgument |> ignore
