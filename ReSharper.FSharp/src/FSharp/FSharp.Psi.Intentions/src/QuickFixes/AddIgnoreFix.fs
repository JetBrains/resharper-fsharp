namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type AddIgnoreFix(node: IFSharpTypeOwnerNode) =
    inherit FSharpModernQuickFixBase()

    let expr = node.As<IFSharpExpression>()

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
            if clauses.Count < 1 then None else

            Some(clauses[0].Expression, "First clause")

        | _ -> None

    let addIgnore (expr: IFSharpExpression) =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())

        let expr = expr.IgnoreParentParens()
        let ignoreApp = expr.CreateElementFactory().CreateIgnoreApp(expr, shouldAddNewLine expr)

        let replaced = ModificationUtil.ReplaceChild(expr, ignoreApp).As<IBinaryAppExpr>()
        addParensIfNeeded replaced.LeftArgument |> ignore

    new (warning: UnitTypeExpectedWarning) =
        AddIgnoreFix(warning.Node)

    new (warning: FunctionValueUnexpectedWarning) =
        AddIgnoreFix(warning.Node)

    new (error: UnitTypeExpectedError) =
        AddIgnoreFix(error.Node)

    override x.Text = "Ignore value"
    override x.IsAvailable _ = isValid expr

    override x.GetCommandSequence() =
        match suggestInnerExpression expr with
        | Some(innerExpression, text) when isNotNull innerExpression ->
            let occurrences =
                [| innerExpression, text
                   expr, "Whole expression" |]
            x.SelectExpression(occurrences, addIgnore)

        | _ -> base.GetCommandSequence()

    override x.ExecutePsiTransaction(_, _) =
        addIgnore expr
        null
