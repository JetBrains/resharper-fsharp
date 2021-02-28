namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText

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
        match suggestInnerExpression expr with
        | Some(innerExpression, text) when isNotNull innerExpression ->
            let occurrences =
                [| innerExpression, text
                   expr, "Whole expression" |]
                |> Array.map (fun (expr, text) ->
                    let getRange (expr: IFSharpExpression) = [| expr.GetNavigationRange() |]
                    WorkflowPopupMenuOccurrence(RichText(text), RichText.Empty, expr, getRange))

            let occurrence =
                let popupMenu = solution.GetComponent<WorkflowPopupMenu>()
                popupMenu.ShowPopup(textControl.Lifetime, occurrences, CustomHighlightingKind.Other, textControl, null)

            expr <- occurrence.Entities |> Seq.tryHead |> Option.toObj
        | _ -> ()

        if isNotNull expr then
            base.Execute(solution, textControl)

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        
        let ignoreApp = expr.CreateElementFactory().CreateIgnoreApp(expr, shouldAddNewLine expr)

        let replaced = ModificationUtil.ReplaceChild(expr, ignoreApp).As<IBinaryAppExpr>()
        addParensIfNeeded replaced.LeftArgument |> ignore
